import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../core/api';
import { DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, toEnDigits, todayIso, fmtMoney } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Badge, Button, Card, CellDef, Chips, Column, ConfirmService, DataTable, FormField, Icon, LoadError, Modal, PageHeader, RowActionsDef, Segmented, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { AccountsSummary, Category, CategoryTotal, Direction, Donor, LedgerPage, LedgerRow } from '../office/office.models';
import { TrPipe, tr } from '../../core/tr';

type Tab = 'ledger' | 'donors';
type Kind = 'donation' | 'expense';
interface EntryForm { date: string; fundId: number; categoryId: number; amount: string; description: string; donorName: string; donorPhone: string; donorAddress: string; }

@Component({
  selector: 'app-accounts-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, FormsModule, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, Badge, Button, Card, CellDef, Chips, DataTable, FormField, Icon, LoadError, Modal, PageHeader, RowActionsDef, Segmented, Skeleton],
  templateUrl: './accounts-page.html',
})
export class AccountsPage {
  private api = inject(OfficeApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private printer = inject(PrintService);

  protected month = signal(todayIso().slice(0, 7));
  protected summary = signal<AccountsSummary | null>(null);
  protected categories = signal<Category[]>([]);
  protected tab = signal<Tab>('ledger');
  protected tabs = [{ value: 'ledger' as Tab, label: tr('আয়-ব্যয় খাতা') }, { value: 'donors' as Tab, label: tr('দাতা') }];

  // ledger filters
  protected fund = signal(0);
  protected dir = signal<'' | Direction>('');
  protected ledger = signal<LedgerPage | null>(null);
  protected donors = signal<Donor[] | null>(null);
  protected fundOptions = computed(() => [{ value: 0, label: tr('সব ফান্ড') }, ...(this.summary()?.funds ?? []).map((f) => ({ value: f.id, label: f.nameBn }))]);
  protected dirOptions = [{ value: '' as '' | Direction, label: tr('সব') }, { value: 'In' as '' | Direction, label: tr('আয়') }, { value: 'Out' as '' | Direction, label: tr('ব্যয়') }];
  protected cols: Column<LedgerRow>[] = [
    { key: 'date', label: tr('তারিখ'), sortable: true },
    { key: 'description', label: tr('বিবরণ'), primary: true, value: (r) => `${r.description ?? ''} ${r.donor ?? ''} ${r.voucherNo ?? ''}` },
    { key: 'category', label: tr('খাত'), sortable: true },
    { key: 'fund', label: tr('ফান্ড'), sortable: true },
    { key: 'amount', label: tr('টাকা'), sortable: true, align: 'end', value: (r) => (r.direction === 'In' ? r.amount : -r.amount) },
  ];
  protected donorCols: Column<Donor>[] = [
    { key: 'name', label: tr('দাতা'), sortable: true, primary: true },
    { key: 'phone', label: tr('মোবাইল') },
    { key: 'count', label: tr('দান'), sortable: true, align: 'end' },
    { key: 'last', label: tr('সর্বশেষ'), sortable: true },
    { key: 'total', label: tr('মোট'), sortable: true, align: 'end' },
  ];

  // entry modal
  protected entryOpen = signal(false);
  protected kind = signal<Kind>('donation');
  protected kinds = [{ value: 'donation' as Kind, label: tr('দান / আয়') }, { value: 'expense' as Kind, label: tr('খরচ') }];
  protected form = signal<EntryForm>({ date: todayIso(), fundId: 1, categoryId: 2, amount: '', description: '', donorName: '', donorPhone: '', donorAddress: '' });
  protected errs = signal<Record<string, string>>({});
  protected saving = signal(false);
  protected kindCats = computed(() => this.categories().filter((c) => (this.kind() === 'donation' ? c.kind === 'Income' && c.id !== 1 && c.id !== 8 : c.kind === 'Expense')));
  protected donorHits = computed(() => {
    const t = this.form().donorName.trim(); const all = this.donors() ?? [];
    return t.length < 2 ? [] : all.filter((d) => d.name.includes(t) || (d.phone ?? '').includes(toEnDigits(t))).slice(0, 5);
  });
  protected selectedFund = computed(() => this.summary()?.funds.find((f) => f.id === +this.form().fundId) ?? null);

  protected loadError = signal<string | null>(null);
  private failed(e: unknown) { this.loadError.set(errorMessage(e, tr('তথ্য লোড করা যায়নি।'))); }
  protected retry() { this.loadError.set(null); this.reload(); }

  constructor() {
    this.api.categories().then((c) => this.categories.set(c)).catch(() => {});
    this.api.donors().then((d) => this.donors.set(d)).catch(() => {});
    effect(() => { const m = this.month(); untracked(() => this.loadSummary(m)); });
    effect(() => { const m = this.month(), f = this.fund(), d = this.dir(); untracked(() => this.loadLedger(m, f, d)); });
  }

  private range(m: string) {
    const [y, mo] = m.split('-').map(Number);
    const last = new Date(Date.UTC(y, mo, 0)).getUTCDate();
    return { from: `${m}-01`, to: `${m}-${String(last).padStart(2, '0')}` };
  }
  private async loadSummary(m: string) { try { this.summary.set(await this.api.summary(m)); } catch (e) { this.failed(e); } }
  private async loadLedger(m: string, fundId: number, d: '' | Direction) {
    this.ledger.set(null);
    try { this.ledger.set(await this.api.ledger({ ...this.range(m), fundId, direction: d || undefined, pageSize: 500 })); }
    catch (e) { this.failed(e); }
  }
  private reload() { this.loadSummary(this.month()); this.loadLedger(this.month(), this.fund(), this.dir()); this.api.donors().then((d) => this.donors.set(d)).catch(() => {}); }

  protected income = computed(() => (this.summary()?.byCategory ?? []).filter((c) => c.kind === 'Income'));
  protected expense = computed(() => (this.summary()?.byCategory ?? []).filter((c) => c.kind === 'Expense'));
  protected barMax = computed(() => Math.max(1, ...(this.summary()?.byCategory ?? []).map((c) => c.amount)));
  protected pct = (c: CategoryTotal) => Math.max(2, Math.round((Math.max(0, c.amount) * 100) / this.barMax()));
  protected totalBalance = computed(() => (this.summary()?.funds ?? []).reduce((a, f) => a + f.balance, 0));

  protected openEntry(k: Kind) {
    this.kind.set(k); this.errs.set({});
    this.form.set({ date: todayIso(), fundId: 1, categoryId: k === 'donation' ? 2 : 4, amount: '', description: '', donorName: '', donorPhone: '', donorAddress: '' });
    this.entryOpen.set(true);
  }
  protected setKind(k: Kind) { this.kind.set(k); this.patch({ categoryId: this.kindCats()[0]?.id ?? 0 }); }
  protected patch(p: Partial<EntryForm>) { this.form.update((f) => ({ ...f, ...p })); }
  protected pickDonor(d: Donor) { this.patch({ donorName: d.name, donorPhone: d.phone ?? '', donorAddress: d.address ?? '' }); }

  protected async saveEntry() {
    const f = this.form();
    const amount = +toEnDigits(String(f.amount)).replace(/,/g, '');
    const e: Record<string, string> = {};
    if (!amount || amount <= 0) e['amount'] = tr('টাকার পরিমাণ লিখুন');
    if (this.kind() === 'expense' && !f.description.trim()) e['description'] = tr('খরচের বিবরণ লিখুন');
    if (Object.keys(e).length) { this.errs.set(e); return; }
    this.saving.set(true); this.errs.set({});
    try {
      const r = await this.api.addEntry({ date: f.date, fundId: +f.fundId, categoryId: +f.categoryId, amount, description: f.description.trim() || null,
        donorName: this.kind() === 'donation' ? f.donorName.trim() || null : null, donorPhone: this.kind() === 'donation' ? toEnDigits(f.donorPhone).trim() || null : null,
        donorAddress: this.kind() === 'donation' ? f.donorAddress.trim() || null : null });
      this.entryOpen.set(false);
      this.toast.success(r.direction === 'In' ? tr('{amt} জমা হয়েছে ({no})', { amt: fmtMoney(r.amount), no: r.voucherNo }) : tr('খরচ লেখা হয়েছে ({no})', { no: r.voucherNo }));
      if (r.direction === 'In' && r.donor) this.printVoucher(r);
      this.reload();
    } catch (err) { this.errs.set(fieldErrors(err)); if (!Object.keys(fieldErrors(err)).length) this.toast.error(errorMessage(err)); }
    finally { this.saving.set(false); }
  }

  protected printVoucher(r: LedgerRow) { this.printer.print({ kind: 'voucher', data: r }, 'a4'); }
  protected async remove(r: LedgerRow) {
    if (!(await this.confirm.ask({ message: tr('এন্ট্রিটি মুছে গেলে ফান্ডের জের বদলে যাবে।'), itemName: `${r.voucherNo} · ${fmtMoney(r.amount)}`, confirmLabel: tr('মুছে ফেলুন') }))) return;
    try { await this.api.deleteEntry(r.id); this.toast.success(tr('এন্ট্রি মুছে ফেলা হয়েছে')); this.reload(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected monthInput(v: string) { if (v) this.month.set(v); }
  protected sourceLabel = (s: string) => (s === 'Fee' ? tr('রসিদ') : s === 'Salary' ? tr('বেতন') : s === 'Library' ? tr('লাইব্রেরি') : '');
}
