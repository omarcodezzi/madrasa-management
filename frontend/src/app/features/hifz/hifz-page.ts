import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { errorMessage, fileUrl } from '../../core/api';
import { DatePipe, formatDate, NumPipe, toBnDigits, toEnDigits, todayIso } from '../../core/format';
import { PreferencesService } from '../../core/preferences.service';
import { ToastService } from '../../core/toast.service';
import { Avatar, Badge, Button, Card, ConfirmService, EmptyState, Icon, IconButton, PageHeader, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { HifzLog, HifzOverview, HifzStudentDetail, JuzCell, PAGES_PER_JUZ, QUALITY_LABEL } from './hifz.models';
import { HifzApi } from './hifz.service';
import { JuzGrid, JuzRing } from './juz-grid';
import { WeekChart } from './week-chart';

interface Form { date: string; juz: string; from: string; to: string; sabakText: string; sabqi: string; manzil: string; mistakes: string; quality: number | null; remarks: string; }

@Component({
  selector: 'app-hifz-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DatePipe, NumPipe, Avatar, Badge, Button, Card, EmptyState, Icon, IconButton, PageHeader, Skeleton, JuzGrid, JuzRing, WeekChart],
  templateUrl: './hifz-page.html',
})
export class HifzPage {
  private api = inject(HifzApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private router = inject(Router);
  private prefs = inject(PreferencesService);
  /** Numbers in the form follow the digit preference (Bangla by default). */
  private d = (n: number | null | undefined) => (n == null ? '' : this.prefs.digits() === 'bn' ? toBnDigits(String(n)) : String(n));

  protected today = todayIso();
  protected overview = signal<HifzOverview | null>(null);
  protected error = signal<string | null>(null);
  protected selectedId = signal<number | null>(+(inject(ActivatedRoute).snapshot.queryParamMap.get('student') ?? 0) || null);
  protected detail = signal<HifzStudentDetail | null>(null);
  protected form = signal<Form>(this.blank());
  protected errs = signal<Record<string, string>>({});
  protected saving = signal(false);
  protected photo = fileUrl;
  protected pages = PAGES_PER_JUZ;
  protected qualities = [5, 4, 3, 2, 1];
  protected qLabel = QUALITY_LABEL;
  protected pending = computed(() => this.overview()?.students.filter((s) => !s.loggedToday) ?? []);
  protected avgJuz = computed(() => { const s = this.overview()?.students ?? []; return s.length ? Math.round((s.reduce((a, b) => a + b.completedJuz, 0) / s.length) * 10) / 10 : 0; });

  constructor() {
    this.loadOverview();
    effect(() => { const id = this.selectedId(); if (id) untracked(() => this.loadStudent(id)); });
  }

  async loadOverview() {
    this.error.set(null);
    try {
      const o = await this.api.overview();
      this.overview.set(o);
      if (!this.selectedId() && o.students.length) this.select(o.students.find((s) => !s.loggedToday)?.studentId ?? o.students[0].studentId);
    } catch (e) { this.error.set(errorMessage(e)); }
  }
  protected select(id: number) {
    this.selectedId.set(id);
    this.router.navigate([], { queryParams: { student: id }, replaceUrl: true });
  }
  private async loadStudent(id: number) {
    this.detail.set(null); this.errs.set({});
    try {
      const d = await this.api.student(id);
      this.detail.set(d);
      this.form.set(d.today ? this.fromLog(d.today) : this.next(d));
    } catch (e) { this.toast.error(errorMessage(e)); }
  }

  private blank(): Form { return { date: this.today, juz: '', from: '', to: '', sabakText: '', sabqi: '', manzil: '', mistakes: this.d(0), quality: null, remarks: '' }; }
  /** Suggest today's sabak: continue from the last page in the current juz. */
  private next(d: HifzStudentDetail): Form {
    const f = this.blank();
    const last = d.logs.find((l) => l.sabakJuz);
    if (last?.sabakJuz) {
      const nextPage = (last.sabakToPage ?? 0) + 1;
      if (nextPage > PAGES_PER_JUZ) { f.juz = this.d(Math.min(30, last.sabakJuz + 1)); f.from = this.d(1); }
      else { f.juz = this.d(last.sabakJuz); f.from = this.d(nextPage); }
      f.sabqi = last.sabqiText ?? ''; f.manzil = last.manzilText ?? '';
    }
    return f;
  }
  private fromLog(l: HifzLog): Form {
    return { date: l.date, juz: this.d(l.sabakJuz), from: this.d(l.sabakFromPage), to: this.d(l.sabakToPage),
      sabakText: l.sabakText ?? '', sabqi: l.sabqiText ?? '', manzil: l.manzilText ?? '', mistakes: this.d(l.mistakes), quality: l.quality ?? null, remarks: l.remarks ?? '' };
  }
  protected patch(p: Partial<Form>) { this.form.update((f) => ({ ...f, ...p })); }
  protected edit(l: HifzLog) { this.form.set(this.fromLog(l)); this.errs.set({}); document.getElementById('hzForm')?.scrollIntoView({ behavior: 'smooth', block: 'start' }); }
  protected err(k: string) { return this.errs()[k] ?? null; }

  protected async save() {
    const id = this.selectedId(); if (!id) return;
    const f = this.form();
    const n = (v: string) => (v.trim() === '' ? null : +toEnDigits(v.trim()));
    this.saving.set(true); this.errs.set({});
    try {
      await this.api.saveLog(id, { date: f.date, sabakJuz: n(f.juz), sabakFromPage: n(f.from), sabakToPage: n(f.to) ?? n(f.from), sabakText: f.sabakText || null,
        sabqiText: f.sabqi || null, manzilText: f.manzil || null, mistakes: n(f.mistakes) ?? 0, quality: f.quality, remarks: f.remarks || null });
      this.toast.success(`${this.detail()?.name} এর পড়া সংরক্ষণ হয়েছে`);
      await this.loadStudent(id);
      const o = await this.api.overview(); this.overview.set(o);
    } catch (e) {
      const fe = fieldErrors(e); this.errs.set(fe);
      if (!Object.keys(fe).length) this.toast.error(errorMessage(e));
    } finally { this.saving.set(false); }
  }

  protected async removeLog(l: HifzLog) {
    const id = this.selectedId(); if (!id) return;
    if (!(await this.confirm.ask({ message: 'এই দিনের সবক, সাবকি ও মানজিলের রেকর্ড মুছে যাবে।', itemName: formatDate(l.date) }))) return;
    try { await this.api.deleteLog(id, l.id); await this.loadStudent(id); this.loadOverview(); } catch (e) { this.toast.error(errorMessage(e)); }
  }

  protected async toggleJuz(c: JuzCell) {
    const id = this.selectedId(); if (!id) return;
    const next = c.status === 'Completed' ? (c.pagesDone && c.pagesDone < PAGES_PER_JUZ ? 'InProgress' : null) : 'Completed';
    if (c.status === 'Completed' && !(await this.confirm.ask({ title: 'পারা সম্পন্ন বাতিল', message: 'এই পারা আর সম্পন্ন হিসেবে দেখাবে না।', itemName: toBnDigits(`পারা ${c.juzNo}`), confirmLabel: 'বাতিল করুন' }))) return;
    try {
      await this.api.setJuz(id, c.juzNo, next);
      if (next === 'Completed') this.toast.success(toBnDigits(`মাশাআল্লাহ! ${c.juzNo} নং পারা সম্পন্ন`));
      await this.loadStudent(id); this.loadOverview();
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
}
