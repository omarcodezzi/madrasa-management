import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../core/api';
import { reportCell, todayIso } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { PreferencesService } from '../../core/preferences.service';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Button, Card, EmptyState, Icon, IconName, PageHeader, Skeleton } from '../../shared/ui';
import { ExamDto } from '../exams/exam.models';
import { ExamsApi } from '../exams/exams.service';
import { OfficeApi } from '../office/office.api';
import { FundBalance, ReportTable } from '../office/office.models';

type Key = 'attendance' | 'collection' | 'dues' | 'results' | 'ledger';
interface Def { key: Key; title: string; desc: string; icon: IconName; filters: ('month' | 'range' | 'class' | 'fund' | 'exam')[]; }
const DEFS: Def[] = [
  { key: 'attendance', title: 'হাজিরা', desc: 'মাসে কে কত দিন উপস্থিত', icon: 'scan', filters: ['month', 'class'] },
  { key: 'collection', title: 'ফি আদায়', desc: 'দিনভিত্তিক আদায়, মাধ্যম অনুযায়ী', icon: 'wallet', filters: ['range'] },
  { key: 'dues', title: 'বকেয়া', desc: 'কার কত মাসের বেতন বাকি', icon: 'alert', filters: ['month', 'class'] },
  { key: 'results', title: 'ফলাফল', desc: 'শ্রেণিভিত্তিক পাসের হার ও প্রথম স্থান', icon: 'exam', filters: ['exam'] },
  { key: 'ledger', title: 'আয়-ব্যয় বিবরণী', desc: 'ফান্ডভিত্তিক জেরসহ হিসাব', icon: 'coins', filters: ['range', 'fund'] },
];

@Component({
  selector: 'app-reports-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, Button, Card, EmptyState, Icon, PageHeader, Skeleton],
  templateUrl: './reports-page.html',
})
export class ReportsPage {
  private api = inject(OfficeApi);
  private exams = inject(ExamsApi);
  private toast = inject(ToastService);
  private printer = inject(PrintService);
  private prefs = inject(PreferencesService);
  protected lookups = inject(LookupsService);

  protected defs = DEFS;
  protected key = signal<Key>('attendance');
  protected def = computed(() => DEFS.find((d) => d.key === this.key())!);
  protected has = (f: Def['filters'][number]) => this.def().filters.includes(f);
  protected month = signal(todayIso().slice(0, 7));
  protected from = signal(todayIso().slice(0, 8) + '01');
  protected to = signal(todayIso());
  protected classId = signal(0);
  protected fundId = signal(0);
  protected examId = signal(0);
  protected examList = signal<ExamDto[]>([]);
  protected funds = signal<FundBalance[]>([]);
  protected table = signal<ReportTable | null>(null);
  protected loading = signal(false);
  protected error = signal<string | null>(null);

  constructor() {
    this.exams.list().then((e) => this.examList.set(e.filter((x) => x.marksEntered > 0))).catch(() => {});
    this.api.summary(todayIso().slice(0, 7)).then((s) => this.funds.set(s.funds)).catch(() => {});
    effect(() => {
      const q = { k: this.key(), month: this.month(), from: this.from(), to: this.to(), classId: this.classId(), fundId: this.fundId(), examId: this.examId() };
      untracked(() => this.run(q.k, q));
    });
  }
  private async run(k: Key, q: { month: string; from: string; to: string; classId: number; fundId: number; examId: number }) {
    const d = DEFS.find((x) => x.key === k)!;
    const p: Record<string, string | number> = {};
    if (d.filters.includes('month')) p['month'] = q.month;
    if (d.filters.includes('range')) { p['from'] = q.from; p['to'] = q.to; }
    if (d.filters.includes('class') && q.classId) p['classId'] = q.classId;
    if (d.filters.includes('fund') && q.fundId) p['fundId'] = q.fundId;
    if (d.filters.includes('exam') && q.examId) p['examId'] = q.examId;
    this.loading.set(true); this.error.set(null);
    try { this.table.set(await this.api.report(k, p)); }
    catch (e) { this.table.set(null); this.error.set(errorMessage(e)); }
    finally { this.loading.set(false); }
  }

  protected cell = (v: unknown, kind: string) => reportCell(v, kind, this.prefs.digits());
  protected isNum = (k: string) => k === 'num' || k === 'money' || k === 'pct';
  protected print() { const t = this.table(); if (t) this.printer.print({ kind: 'report', data: t }); }
  protected csv() {
    const t = this.table(); if (!t) return;
    const esc = (s: string) => (/[",\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s);
    const line = (r: Record<string, unknown>) => t.columns.map((c) => esc(reportCell(r[c.key], c.kind, 'en').replace(/^৳ /, ''))).join(',');
    const rows = [t.columns.map((c) => esc(c.label)).join(','), ...t.rows.map(line), ...(t.totals ? [line(t.totals)] : [])];
    // BOM so Excel opens Bangla text correctly
    const blob = new Blob(['﻿' + rows.join('\r\n')], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a'); a.href = URL.createObjectURL(blob); a.download = `${t.key}-${todayIso()}.csv`; a.click(); URL.revokeObjectURL(a.href);
  }
}
