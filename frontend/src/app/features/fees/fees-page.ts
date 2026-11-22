import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { errorMessage, PaymentMethod } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, toEnDigits, todayIso, fmtMoney } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Avatar, Badge, Button, Card, CellDef, Chips, Column, ConfirmService, DataTable, EmptyState, Icon, LoadError, Modal, PageHeader, RowActionsDef, Segmented, Skeleton } from '../../shared/ui';
import { StudentRow } from '../students/student.models';
import { StudentsApi } from '../students/students.service';
import { DueListRow, METHOD_LABEL, MonthSummary, Receipt, ReceiptSummary, StudentDues } from './fee.models';
import { FeesApi } from './fees.service';
import { ReceiptView } from './receipt-view';
import { TrPipe, tr } from '../../core/tr';

type Tab = 'collect' | 'dues' | 'receipts';

@Component({
  selector: 'app-fees-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, FormsModule, RouterLink, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, Avatar, Badge, Button, Card, CellDef, Chips, DataTable, EmptyState, Icon, LoadError, Modal, PageHeader, RowActionsDef, Segmented, Skeleton, ReceiptView],
  templateUrl: './fees-page.html',
})
export class FeesPage {
  private api = inject(FeesApi);
  private students = inject(StudentsApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private printer = inject(PrintService);
  private router = inject(Router);
  private auth = inject(AuthService);
  protected lookups = inject(LookupsService);

  protected month = signal(todayIso().slice(0, 7));
  protected tab = signal<Tab>('collect');
  protected tabs = [{ value: 'collect' as Tab, label: tr('আদায়') }, { value: 'dues' as Tab, label: tr('বকেয়া তালিকা') }, { value: 'receipts' as Tab, label: tr('রসিদ') }];
  protected summary = signal<MonthSummary | null>(null);
  protected canVoid = computed(() => this.auth.can(P.accounts));

  // Collect
  protected all = signal<StudentRow[]>([]);
  protected q = signal('');
  protected matches = computed(() => {
    const t = toEnDigits(this.q().trim()).toLowerCase();
    if (!t) return [];
    return this.all().filter((s) => s.name.toLowerCase().includes(t) || s.studentCode.toLowerCase().includes(t)
      || String(s.roll) === t || (s.phone ?? '').replace(/\D/g, '').includes(t.replace(/\D/g, '') || '#')).slice(0, 8);
  });
  protected dues = signal<StudentDues | null>(null);
  protected loadingDues = signal(false);
  protected picked = signal<Record<number, number>>({});
  protected method = signal<PaymentMethod>('Cash');
  protected methods = (Object.keys(METHOD_LABEL) as PaymentMethod[]).map((m) => ({ value: m, label: METHOD_LABEL[m] }));
  protected reference = signal('');
  protected total = computed(() => Object.values(this.picked()).reduce((a, b) => a + (+b || 0), 0));
  protected refError = signal<string | null>(null);
  protected collecting = signal(false);

  // Receipt modal
  protected receipt = signal<Receipt | null>(null);
  protected receiptOpen = signal(false);

  // Lists
  protected dueClass = signal(0);
  protected dueRows = signal<DueListRow[] | null>(null);
  protected receiptRows = signal<ReceiptSummary[] | null>(null);
  protected classOptions = computed(() => [{ value: 0, label: tr('সব শ্রেণি') }, ...this.lookups.classes().map((c) => ({ value: c.id, label: c.nameBn }))]);
  protected dueCols: Column<DueListRow>[] = [
    { key: 'name', label: tr('শিক্ষার্থী'), sortable: true },
    { key: 'className', label: tr('শ্রেণি'), sortable: true },
    { key: 'monthsDue', label: tr('মাস'), sortable: true, align: 'end' },
    { key: 'oldestMonth', label: tr('সবচেয়ে পুরনো'), sortable: true },
    { key: 'totalDue', label: tr('বকেয়া'), sortable: true, align: 'end' },
    { key: 'phone', label: tr('মোবাইল') },
  ];
  protected receiptCols: Column<ReceiptSummary>[] = [
    { key: 'receiptNo', label: tr('রসিদ নং'), sortable: true },
    { key: 'date', label: tr('তারিখ'), sortable: true },
    { key: 'studentName', label: tr('শিক্ষার্থী'), sortable: true },
    { key: 'months', label: tr('মাস') },
    { key: 'method', label: tr('মাধ্যম'), value: (r) => METHOD_LABEL[r.method] },
    { key: 'totalPaid', label: tr('টাকা'), sortable: true, align: 'end' },
  ];
  protected mLabel = (m: string) => (METHOD_LABEL as Record<string, string>)[m] ?? m;
  protected dueTrack = (r: DueListRow) => r.studentId;

  protected loadError = signal<string | null>(null);
  private failed(e: unknown) { this.loadError.set(errorMessage(e, tr('তথ্য লোড করা যায়নি।'))); }
  protected retry() { this.loadError.set(null); this.refresh(this.month()); this.loadList(this.tab(), this.month(), this.dueClass()); }

  constructor() {
    this.students.all({ status: 'Active' }).then((r) => this.all.set(r.items)).catch(() => {});
    const sid = +(inject(ActivatedRoute).snapshot.queryParamMap.get('student') ?? 0);
    if (sid) this.pick(sid);
    effect(() => { const m = this.month(); untracked(() => this.refresh(m)); });
    effect(() => { const t = this.tab(), m = this.month(), c = this.dueClass(); untracked(() => this.loadList(t, m, c)); });
  }

  private refresh(m: string) { this.api.summary(m).then((s) => this.summary.set(s)).catch((e) => this.failed(e)); }
  private async loadList(t: Tab, m: string, c: number) {
    try {
      if (t === 'dues') { this.dueRows.set(null); this.dueRows.set(await this.api.dues(m, c)); }
      if (t === 'receipts') { this.receiptRows.set(null); this.receiptRows.set((await this.api.receipts(m)).items); }
    } catch (e) { this.failed(e); }
  }

  protected async pick(id: number) {
    this.q.set(''); this.tab.set('collect'); this.loadingDues.set(true); this.refError.set(null);
    try {
      const d = await this.api.student(id);
      this.dues.set(d);
      // Pre-tick everything up to the selected month, the common case at the counter
      this.picked.set(Object.fromEntries(d.items.filter((i) => !i.month || i.month <= this.month()).map((i) => [i.invoiceId, i.due])));
      this.router.navigate([], { queryParams: { student: id }, replaceUrl: true });
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.loadingDues.set(false); }
  }
  protected clear() { this.dues.set(null); this.picked.set({}); this.reference.set(''); this.router.navigate([], { queryParams: {}, replaceUrl: true }); }
  protected toggle(id: number, due: number) {
    this.picked.update((p) => { const n = { ...p }; if (id in n) delete n[id]; else n[id] = due; return n; });
  }
  protected setAmount(id: number, v: string, max: number) {
    const n = Math.max(0, Math.min(max, Math.round(+toEnDigits(v) || 0)));
    this.picked.update((p) => ({ ...p, [id]: n }));
  }

  protected async collect() {
    const d = this.dues(); if (!d) return;
    const lines = Object.entries(this.picked()).filter(([, a]) => a > 0).map(([id, amount]) => ({ invoiceId: +id, amount }));
    if (!lines.length) { this.toast.error(tr('অন্তত একটি খাত বাছাই করুন')); return; }
    if (this.method() !== 'Cash' && !this.reference().trim()) { this.refError.set(tr('বিকাশ/নগদ/ব্যাংকের ক্ষেত্রে ট্রানজেকশন নম্বর দিন')); return; }
    this.refError.set(null); this.collecting.set(true);
    try {
      const r = await this.api.collect({ studentId: d.studentId, lines, method: this.method(), reference: this.reference().trim() || null });
      this.toast.success(tr('{name} এর {amt} জমা হয়েছে', { name: r.studentName, amt: fmtMoney(r.totalPaid) }));
      this.showReceipt(r);
      this.reference.set(''); this.method.set('Cash');
      this.pick(d.studentId); this.refresh(this.month());
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.collecting.set(false); }
  }

  protected showReceipt(r: Receipt) { this.receipt.set(r); this.receiptOpen.set(true); }
  protected async openReceipt(no: number) { try { this.showReceipt(await this.api.receipt(no)); } catch (e) { this.toast.error(errorMessage(e)); } }
  protected print(mode: 'a4' | 'pos') { const r = this.receipt(); if (r) this.printer.print({ kind: 'receipt', data: r }, mode); }
  protected async voidReceipt() {
    const r = this.receipt(); if (!r) return;
    if (!(await this.confirm.ask({ message: tr('রসিদটি বাতিল হলে টাকা আবার বকেয়া হিসেবে দেখাবে এবং হিসাবে উল্টো এন্ট্রি হবে।'), itemName: tr('রসিদ {no}', { no: r.receiptNo }), confirmLabel: tr('রসিদ বাতিল করুন') }))) return;
    try {
      this.receipt.set(await this.api.void(r.receiptNo));
      this.toast.success(tr('রসিদ বাতিল হয়েছে'));
      this.refresh(this.month()); this.loadList(this.tab(), this.month(), this.dueClass());
      if (this.dues()?.studentId === r.studentId) this.pick(r.studentId);
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected monthInput(v: string) { if (v) this.month.set(v); }
  protected percent = computed(() => { const s = this.summary(); return s && s.expected ? Math.min(100, Math.round((s.collectedForMonth * 100) / s.expected)) : 0; });
}
