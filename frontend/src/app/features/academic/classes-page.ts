import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { MoneyPipe, NumPipe, TimePipe } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { ToastService } from '../../core/toast.service';
import { Badge, Button, Card, ConfirmService, EmptyState, Icon, IconButton, Modal, PageHeader, Segmented, Skeleton } from '../../shared/ui';
import { ClassOverview, ClassSubject, DAY_NAME, DepartmentOverview, Period, Routine, SubjectDto, TeacherDto } from './academic.models';
import { AcademicApi, fieldErrors } from './academic.service';

type Tab = 'subjects' | 'routine';

@Component({
  selector: 'app-classes-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MoneyPipe, NumPipe, TimePipe, Badge, Button, Card, EmptyState, Icon, IconButton, Modal, PageHeader, Segmented, Skeleton],
  templateUrl: './classes-page.html',
})
export class ClassesPage {
  private api = inject(AcademicApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private lookups = inject(LookupsService);
  private auth = inject(AuthService);
  protected canEdit = computed(() => this.auth.can(P.settings));

  protected depts = signal<DepartmentOverview[] | null>(null);
  protected error = signal<string | null>(null);
  protected selectedId = signal<number | null>(null);
  protected selected = computed(() => this.depts()?.flatMap((d) => d.classes).find((c) => c.id === this.selectedId()) ?? null);
  protected deptOf = computed(() => this.depts()?.find((d) => d.id === this.selected()?.departmentId) ?? null);
  protected tab = signal<Tab>('subjects');
  protected tabs = [{ value: 'subjects' as Tab, label: 'কিতাব ও উস্তাদ' }, { value: 'routine' as Tab, label: 'সাপ্তাহিক রুটিন' }];

  protected subjects = signal<ClassSubject[] | null>(null);
  protected allSubjects = signal<SubjectDto[]>([]);
  protected teachers = signal<TeacherDto[]>([]);
  protected routine = signal<Routine | null>(null);
  protected dayName = DAY_NAME;

  // Class modal
  protected classOpen = signal(false);
  protected classForm = signal({ id: 0, departmentId: 0, nameBn: '', nameAr: '', monthlyFee: '' as string | number });
  protected classErr = signal<Record<string, string>>({});
  protected savingClass = signal(false);

  // Kitab modal
  protected subjOpen = signal(false);
  protected subjForm = signal({ id: 0, subjectId: 0 as number, newName: '', newNameAr: '', teacherId: 0 as number, fullMarks: 100, passMarks: 33 });
  protected subjErr = signal<Record<string, string>>({});
  protected savingSubj = signal(false);

  // Routine editor
  protected editing = signal(false);
  protected draftPeriods = signal<Period[]>([]);
  protected draftCells = signal<Record<string, number>>({});
  protected routineErr = signal<Record<string, string>>({});
  protected savingRoutine = signal(false);

  constructor() {
    this.load();
    this.api.subjects().then((s) => this.allSubjects.set(s)).catch(() => {});
    this.api.teachers().then((t) => this.teachers.set(t)).catch(() => {});
    effect(() => { const id = this.selectedId(); if (id) untracked(() => this.loadClass(id)); });
  }

  async load() {
    this.error.set(null);
    try {
      const d = await this.api.overview();
      this.depts.set(d);
      if (!this.selectedId()) this.selectedId.set(d.flatMap((x) => x.classes)[0]?.id ?? null);
    } catch (e) { this.error.set(errorMessage(e)); }
  }
  private async loadClass(id: number) {
    this.subjects.set(null); this.routine.set(null); this.editing.set(false);
    try {
      const [s, r] = await Promise.all([this.api.classSubjects(id), this.api.routine(id)]);
      this.subjects.set(s); this.routine.set(r);
    } catch (e) { this.toast.error(errorMessage(e)); }
  }

  // ---- class ----
  protected newClass() {
    this.classErr.set({});
    this.classForm.set({ id: 0, departmentId: this.deptOf()?.id ?? this.depts()?.[0]?.id ?? 0, nameBn: '', nameAr: '', monthlyFee: '' });
    this.classOpen.set(true);
  }
  protected editClass(c: ClassOverview) {
    this.classErr.set({});
    this.classForm.set({ id: c.id, departmentId: c.departmentId, nameBn: c.nameBn, nameAr: c.nameAr ?? '', monthlyFee: c.monthlyFee ?? '' });
    this.classOpen.set(true);
  }
  protected patchClass(p: Partial<ReturnType<typeof this.classForm>>) { this.classForm.update((f) => ({ ...f, ...p })); }
  protected async saveClass() {
    const f = this.classForm();
    this.savingClass.set(true); this.classErr.set({});
    try {
      const input = { departmentId: +f.departmentId, nameBn: f.nameBn, nameAr: f.nameAr || null, monthlyFee: f.monthlyFee === '' ? null : +f.monthlyFee };
      const r = f.id ? await this.api.updateClass(f.id, input) : await this.api.createClass(input);
      this.toast.success(f.id ? 'শ্রেণির তথ্য হালনাগাদ হয়েছে' : `${f.nameBn} শ্রেণি যোগ হয়েছে`);
      this.classOpen.set(false);
      await this.load(); this.lookups.reload();
      this.selectedId.set(r.id);
    } catch (e) { this.classErr.set(fieldErrors(e)); if (!Object.keys(fieldErrors(e)).length) this.toast.error(errorMessage(e)); }
    finally { this.savingClass.set(false); }
  }
  protected async removeClass(c: ClassOverview) {
    if (!(await this.confirm.ask({ message: 'শ্রেণিটি, এর কিতাব ও রুটিন মুছে যাবে।', itemName: c.nameBn }))) return;
    try {
      await this.api.deleteClass(c.id);
      this.toast.success(`${c.nameBn} মুছে ফেলা হয়েছে`);
      this.selectedId.set(null); await this.load(); this.lookups.reload();
    } catch (e) { this.toast.error(errorMessage(e)); }
  }

  // ---- kitab ----
  protected newSubject() {
    this.subjErr.set({});
    this.subjForm.set({ id: 0, subjectId: 0, newName: '', newNameAr: '', teacherId: 0, fullMarks: 100, passMarks: 33 });
    this.subjOpen.set(true);
  }
  protected editSubject(s: ClassSubject) {
    this.subjErr.set({});
    this.subjForm.set({ id: s.id, subjectId: s.subjectId, newName: '', newNameAr: '', teacherId: s.teacherId ?? 0, fullMarks: s.fullMarks, passMarks: s.passMarks });
    this.subjOpen.set(true);
  }
  protected patchSubj(p: Partial<ReturnType<typeof this.subjForm>>) { this.subjForm.update((f) => ({ ...f, ...p })); }
  protected async saveSubject() {
    const c = this.selected(); if (!c) return;
    const f = this.subjForm();
    this.savingSubj.set(true); this.subjErr.set({});
    try {
      await this.api.saveClassSubject(c.id, f.id || null, {
        subjectId: +f.subjectId || null, newSubjectName: +f.subjectId ? null : f.newName, newSubjectNameAr: +f.subjectId ? null : f.newNameAr || null,
        teacherId: +f.teacherId || null, fullMarks: +f.fullMarks, passMarks: +f.passMarks,
      });
      this.toast.success('কিতাব সংরক্ষণ হয়েছে');
      this.subjOpen.set(false);
      this.subjects.set(await this.api.classSubjects(c.id));
      this.api.subjects().then((s) => this.allSubjects.set(s));
      this.load();
    } catch (e) { this.subjErr.set(fieldErrors(e)); if (!Object.keys(fieldErrors(e)).length) this.toast.error(errorMessage(e)); }
    finally { this.savingSubj.set(false); }
  }
  protected async removeSubject(s: ClassSubject) {
    const c = this.selected(); if (!c) return;
    if (!(await this.confirm.ask({ message: 'কিতাবটি এই শ্রেণি ও রুটিন থেকে সরানো হবে।', itemName: s.subjectName, confirmLabel: 'সরিয়ে দিন' }))) return;
    try {
      await this.api.removeClassSubject(c.id, s.id);
      this.subjects.set(await this.api.classSubjects(c.id));
      this.routine.set(await this.api.routine(c.id));
      this.load();
    } catch (e) { this.toast.error(errorMessage(e)); }
  }

  // ---- routine ----
  protected cell(day: number, period: number) { return this.routine()?.slots.find((s) => s.day === day && s.periodNo === period) ?? null; }
  protected startEdit() {
    const r = this.routine(); if (!r) return;
    this.draftPeriods.set(r.periods.map((p) => ({ ...p, start: p.start.slice(0, 5), end: p.end.slice(0, 5) })));
    this.draftCells.set(Object.fromEntries(r.slots.map((s) => [`${s.day},${s.periodNo}`, s.classSubjectId])));
    this.routineErr.set({}); this.editing.set(true);
  }
  protected setCell(day: number, period: number, v: string) {
    this.draftCells.update((c) => { const n = { ...c }; if (+v) n[`${day},${period}`] = +v; else delete n[`${day},${period}`]; return n; });
  }
  protected setPeriod(i: number, key: 'start' | 'end', v: string) { this.draftPeriods.update((p) => p.map((x, j) => (j === i ? { ...x, [key]: v } : x))); }
  protected addPeriod() {
    this.draftPeriods.update((p) => {
      const last = p[p.length - 1];
      const start = last?.end ?? '07:30';
      const [h, m] = start.split(':').map(Number); const t = h * 60 + m + 45;
      return [...p, { periodNo: (last?.periodNo ?? 0) + 1, start, end: `${String(Math.floor(t / 60) % 24).padStart(2, '0')}:${String(t % 60).padStart(2, '0')}` }];
    });
  }
  protected removePeriod() {
    const last = this.draftPeriods().at(-1); if (!last) return;
    this.draftPeriods.update((p) => p.slice(0, -1));
    this.draftCells.update((c) => Object.fromEntries(Object.entries(c).filter(([k]) => !k.endsWith(`,${last.periodNo}`))));
  }
  protected async saveRoutine() {
    const c = this.selected(); if (!c) return;
    this.savingRoutine.set(true); this.routineErr.set({});
    const periods = this.draftPeriods().map((p) => ({ periodNo: p.periodNo, start: p.start.length === 5 ? p.start + ':00' : p.start, end: p.end.length === 5 ? p.end + ':00' : p.end }));
    const slots = Object.entries(this.draftCells()).map(([k, id]) => { const [day, periodNo] = k.split(',').map(Number); return { day, periodNo, classSubjectId: id }; });
    try {
      this.routine.set(await this.api.saveRoutine(c.id, periods, slots));
      this.editing.set(false);
      this.toast.success('রুটিন সংরক্ষণ হয়েছে');
    } catch (e) {
      const errs = fieldErrors(e);
      this.routineErr.set(errs);
      this.toast.error(Object.values(errs)[0] ?? errorMessage(e));
    } finally { this.savingRoutine.set(false); }
  }
  protected slotErr(day: number, period: number) { return this.routineErr()[`slot[${day},${period}]`] ?? null; }
  protected periodErr(no: number) { return this.routineErr()[`periods[${no}]`] ?? null; }
  /** Per-kitab weekly count, a quick sanity check for the office. */
  protected weekly = computed(() => {
    const r = this.routine(); if (!r) return [];
    const m = new Map<string, number>();
    for (const s of r.slots) m.set(s.subjectName, (m.get(s.subjectName) ?? 0) + 1);
    return [...m.entries()].map(([name, n]) => ({ name, n }));
  });
}
