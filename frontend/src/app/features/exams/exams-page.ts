import { ChangeDetectionStrategy, Component, computed, effect, inject, OnDestroy, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { errorMessage } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { DatePipe, NumPipe, toBnDigits, toEnDigits } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { PreferencesService } from '../../core/preferences.service';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Badge, Button, Card, Chips, ConfirmService, EmptyState, Icon, IconButton, Modal, PageHeader, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { StudentsApi } from '../students/students.service';
import { computeResults, ExamDto, gradeTone, MarkCell, ResultSheet } from './exam.models';
import { ExamsApi } from './exams.service';

type Save = 'idle' | 'saving' | 'saved' | 'error';

@Component({
  selector: 'app-exams-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DatePipe, NumPipe, Badge, Button, Card, Chips, EmptyState, Icon, IconButton, Modal, PageHeader, Skeleton],
  templateUrl: './exams-page.html',
})
export class ExamsPage implements OnDestroy {
  private api = inject(ExamsApi);
  private students = inject(StudentsApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private printer = inject(PrintService);
  private router = inject(Router);
  private prefs = inject(PreferencesService);
  private auth = inject(AuthService);
  protected lookups = inject(LookupsService);
  protected canManage = computed(() => this.auth.can(P.examsPublish));

  private qp = inject(ActivatedRoute).snapshot.queryParamMap;
  protected exams = signal<ExamDto[] | null>(null);
  protected examId = signal<number | null>(+(this.qp.get('exam') ?? 0) || null);
  protected exam = computed(() => this.exams()?.find((e) => e.id === this.examId()) ?? null);
  protected classId = signal<number>(+(this.qp.get('class') ?? 0) || 0);
  protected classOptions = computed(() => this.lookups.classes().map((c) => ({ value: c.id, label: c.nameBn })));
  protected sheet = signal<ResultSheet | null>(null);
  protected error = signal<string | null>(null);

  /** Raw text typed in each cell, keyed "studentId:classSubjectId". */
  protected input = signal<Record<string, string>>({});
  private dirty = new Set<string>();
  protected cellErr = signal<Record<string, string>>({});
  protected save = signal<Save>('idle');
  private timer?: ReturnType<typeof setTimeout>;

  protected rows = computed(() => [...(this.sheet()?.rows ?? [])].sort((a, b) => a.roll - b.roll));
  protected live = computed(() => {
    const s = this.sheet(); if (!s) return new Map();
    const inp = this.input();
    const rows = s.rows.map((r) => ({ studentId: r.studentId, cells: s.subjects.map((sub) => this.parse(inp[`${r.studentId}:${sub.id}`] ?? '')).map((p, i) => ({ classSubjectId: s.subjects[i].id, ...p })) }));
    return computeResults(s.subjects, rows, s.grades);
  });
  protected merit = computed(() => [...this.rows()].filter((r) => this.live().get(r.studentId)?.merit).sort((a, b) => this.live().get(a.studentId)!.merit! - this.live().get(b.studentId)!.merit!));
  protected gradeCounts = computed(() => {
    const s = this.sheet(); if (!s) return [];
    const vals = [...this.live().values()];
    return [...s.grades].sort((a, b) => b.minAverage - a.minAverage).map((g) => ({ g, n: vals.filter((v) => v.grade?.id === g.id).length }));
  });
  protected tone = (g: string | null | undefined) => gradeTone(g, this.sheet()?.grades ?? []);
  protected progress = (e: ExamDto) => (e.marksExpected ? Math.min(100, Math.round((e.marksEntered * 100) / e.marksExpected)) : 0);

  // Exam modal
  protected examOpen = signal(false);
  protected examForm = signal({ id: 0, name: '', startDate: '' });
  protected examErr = signal<Record<string, string>>({});
  protected savingExam = signal(false);

  constructor() {
    this.loadExams();
    effect(() => {
      const c = this.lookups.classes();
      if (!this.classId() && c.length) untracked(() => this.classId.set(c[0].id));
    });
    effect(() => {
      const e = this.examId(), c = this.classId();
      if (e && c) untracked(() => this.loadSheet(e, c));
    });
  }

  async loadExams() {
    this.error.set(null);
    try {
      const list = await this.api.list();
      this.exams.set(list);
      if (!this.exam()) this.examId.set((list.find((e) => !e.isPublished && e.marksEntered) ?? list.find((e) => e.isPublished) ?? list[0])?.id ?? null);
    } catch (e) { this.error.set(errorMessage(e)); }
  }
  private async loadSheet(examId: number, classId: number) {
    await this.flush();
    this.sheet.set(null); this.cellErr.set({});
    this.router.navigate([], { queryParams: { exam: examId, class: classId }, replaceUrl: true });
    try {
      const s = await this.api.results(examId, classId);
      const inp: Record<string, string> = {};
      for (const r of s.rows) for (const c of r.cells) inp[`${r.studentId}:${c.classSubjectId}`] = this.show(c);
      this.input.set(inp); this.dirty.clear(); this.save.set('idle');
      this.sheet.set(s);
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected pickExam(id: number) { if (id !== this.examId()) this.examId.set(id); }
  protected pickClass(id: number) { if (id !== this.classId()) this.classId.set(id); }

  // ---- marks entry ----
  private show(c: MarkCell) { return c.isAbsent ? 'অ' : c.marks == null ? '' : this.prefs.digits() === 'bn' ? toBnDigits(String(c.marks)) : String(c.marks); }
  private parse(v: string): { marks: number | null; isAbsent: boolean } {
    const t = toEnDigits(v.trim());
    if (!t) return { marks: null, isAbsent: false };
    if (/^(অ|a|ab|abs|অনু)/i.test(t)) return { marks: null, isAbsent: true };
    const n = Number(t);
    return { marks: Number.isFinite(n) ? n : null, isAbsent: false };
  }
  protected value(sid: number, cs: number) { return this.input()[`${sid}:${cs}`] ?? ''; }
  protected bad(sid: number, cs: number, pass: number, full: number) {
    const k = `${sid}:${cs}`;
    if (this.cellErr()[k]) return true;
    const p = this.parse(this.input()[k] ?? '');
    return p.isAbsent || (p.marks != null && (p.marks < pass || p.marks > full));
  }
  protected onInput(sid: number, cs: number, v: string) {
    const k = `${sid}:${cs}`;
    this.input.update((m) => ({ ...m, [k]: v }));
    this.dirty.add(k);
    if (this.cellErr()[k]) this.cellErr.update((e) => { const n = { ...e }; delete n[k]; return n; });
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.flush(), 900);
  }
  /** On leaving a cell, show the number in the preferred digits (৮৫ even if typed 85). */
  protected tidy(sid: number, cs: number) {
    const k = `${sid}:${cs}`; const raw = this.input()[k] ?? '';
    const p = this.parse(raw);
    if (!raw.trim() || this.cellErr()[k] || (!p.isAbsent && p.marks == null)) return;
    const shown = this.show({ classSubjectId: cs, ...p });
    if (shown !== raw) this.input.update((m) => ({ ...m, [k]: shown }));
  }
  /** Enter / ↓ moves down the column (how teachers enter a whole kitab at once); ↑ moves up. */
  protected onKey(e: KeyboardEvent, row: number, col: number) {
    const go = (r: number, c: number) => { const el = document.getElementById(`mk-${r}-${c}`) as HTMLInputElement | null; if (el) { e.preventDefault(); el.focus(); el.select(); } };
    if (e.key === 'Enter' || e.key === 'ArrowDown') go(row + 1, col);
    else if (e.key === 'ArrowUp') go(row - 1, col);
    else if (e.key === 'ArrowRight' && (e.target as HTMLInputElement).selectionStart === (e.target as HTMLInputElement).value.length) go(row, col + 1);
    else if (e.key === 'ArrowLeft' && (e.target as HTMLInputElement).selectionStart === 0) go(row, col - 1);
  }
  async flush() {
    clearTimeout(this.timer);
    const s = this.sheet(); if (!s || !this.dirty.size) return;
    const keys = [...this.dirty]; this.dirty.clear();
    const entries = keys.map((k) => { const [studentId, classSubjectId] = k.split(':').map(Number); return { studentId, classSubjectId, ...this.parse(this.input()[k] ?? '') }; });
    // Bad input stays on screen, flagged, and is not sent
    const local: Record<string, string> = {};
    for (const e of entries) {
      const sub = s.subjects.find((x) => x.id === e.classSubjectId)!;
      const raw = toEnDigits((this.input()[`${e.studentId}:${e.classSubjectId}`] ?? '').trim());
      if (raw && !e.isAbsent && (e.marks == null || e.marks < 0 || e.marks > sub.fullMarks)) local[`${e.studentId}:${e.classSubjectId}`] = `০ থেকে ${toBnDigits(String(sub.fullMarks))} এর মধ্যে নম্বর দিন, অনুপস্থিত হলে "অ" লিখুন`;
    }
    const send = entries.filter((e) => !local[`${e.studentId}:${e.classSubjectId}`]);
    this.cellErr.update((x) => ({ ...x, ...local }));
    if (!send.length) { this.save.set('error'); return; }
    this.save.set('saving');
    try {
      await this.api.saveMarks(s.examId, s.classId, send);
      this.save.set(Object.keys(local).length ? 'error' : 'saved');
      this.api.list().then((l) => this.exams.set(l)).catch(() => {});
    } catch (e) {
      const fe = fieldErrors(e);
      this.cellErr.update((x) => ({ ...x, ...fe }));
      for (const k of keys) this.dirty.add(k);
      this.save.set('error');
      this.toast.error(errorMessage(e));
    }
  }
  /** Route guard: save what is pending; only ask if some marks could not be saved. */
  async leave() {
    await this.flush();
    return this.save() !== 'error' || window.confirm('কিছু নম্বর সংরক্ষণ হয়নি (লাল ঘর)। পাতা ছেড়ে গেলে সেগুলো হারিয়ে যাবে। তবুও যাবেন?');
  }
  ngOnDestroy() { this.flush(); }
  protected errText = computed(() => Object.values(this.cellErr())[0] ?? null);

  // ---- exams ----
  protected newExam() { this.examErr.set({}); this.examForm.set({ id: 0, name: '', startDate: '' }); this.examOpen.set(true); }
  protected editExam(e: ExamDto) { this.examErr.set({}); this.examForm.set({ id: e.id, name: e.name, startDate: e.startDate ?? '' }); this.examOpen.set(true); }
  protected patchExam(p: Partial<ReturnType<typeof this.examForm>>) { this.examForm.update((f) => ({ ...f, ...p })); }
  protected async saveExam() {
    const f = this.examForm();
    this.savingExam.set(true); this.examErr.set({});
    try {
      const e = await this.api.save(f.id || null, { name: f.name, startDate: f.startDate || null });
      this.examOpen.set(false);
      this.toast.success(f.id ? 'পরীক্ষার তথ্য হালনাগাদ হয়েছে' : `${e.name} যোগ হয়েছে`);
      await this.loadExams(); this.examId.set(e.id);
    } catch (e) { const fe = fieldErrors(e); this.examErr.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.savingExam.set(false); }
  }
  protected async removeExam(e: ExamDto) {
    if (!(await this.confirm.ask({ message: 'পরীক্ষাটি মুছে যাবে। নম্বর দেওয়া থাকলে মুছা যাবে না।', itemName: e.name }))) return;
    try { await this.api.remove(e.id); this.examId.set(null); this.sheet.set(null); await this.loadExams(); }
    catch (err) { this.toast.error(errorMessage(err)); }
  }
  protected async publish(value: boolean) {
    const e = this.exam(); if (!e) return;
    await this.flush();
    const msg = value ? 'ফলাফল প্রকাশ হলে নম্বর আর বদলানো যাবে না, আর মার্কশিট ও অভিভাবক পোর্টালে ফলাফল দেখা যাবে।' : 'প্রকাশ বাতিল করলে নম্বর আবার বদলানো যাবে।';
    if (!(await this.confirm.ask({ title: value ? 'ফলাফল প্রকাশ' : 'প্রকাশ বাতিল', message: msg, itemName: e.name, confirmLabel: value ? 'প্রকাশ করুন' : 'প্রকাশ বাতিল করুন' }))) return;
    try {
      const updated = await this.api.publish(e.id, value);
      this.exams.update((l) => l?.map((x) => (x.id === e.id ? updated : x)) ?? l);
      this.toast.success(value ? 'ফলাফল প্রকাশিত হয়েছে' : 'প্রকাশ বাতিল হয়েছে');
      this.loadSheet(e.id, this.classId());
    } catch (err) { this.toast.error(errorMessage(err)); }
  }

  // ---- printing ----
  protected async printSheet() {
    await this.flush();
    const s = this.sheet(); if (!s) return;
    this.printer.print({ kind: 'tabulation', data: await this.api.results(s.examId, s.classId) });
  }
  protected async printMarkSheet(studentId: number) {
    await this.flush();
    const s = this.sheet(); if (!s) return;
    try {
      const [res, st] = await Promise.all([this.api.student(studentId, s.examId), this.students.get(studentId)]);
      if (!res.length) { this.toast.error('এই শিক্ষার্থীর কোনো নম্বর নেই'); return; }
      this.printer.print({ kind: 'marksheet', data: [{ student: st, result: res[0] }] });
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected async printAllMarkSheets() {
    await this.flush();
    const s = this.sheet(); if (!s) return;
    try {
      const list = await Promise.all(this.rows().map(async (r) => ({ student: { name: r.name, studentCode: r.studentCode, roll: r.roll }, result: (await this.api.student(r.studentId, s.examId))[0] })));
      this.printer.print({ kind: 'marksheet', data: list.filter((x) => x.result) });
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
}
