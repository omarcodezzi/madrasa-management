import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttendanceStatus, errorMessage, fileUrl } from '../../core/api';
import { DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, TimePipe, toEnDigits, todayIso } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Avatar, Badge, BadgeTone, Button, Card, CellDef, Column, ConfirmService, DataTable, EmptyState, FormField, Icon, LoadError, Modal, PageHeader, Segmented, Skeleton, resizePhoto } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { FundBalance, SalaryRow, StaffDayRow, StaffDetail, StaffInput, StaffRow } from '../office/office.models';

type Tab = 'list' | 'attendance' | 'salary';
const STATUS: { value: AttendanceStatus; label: string; tone: BadgeTone }[] = [
  { value: 'Present', label: 'উপস্থিত', tone: 'success' }, { value: 'Late', label: 'দেরি', tone: 'warning' },
  { value: 'Absent', label: 'অনুপস্থিত', tone: 'danger' }, { value: 'Leave', label: 'ছুটি', tone: 'info' },
];
const blank = (): StaffInput => ({ name: '', designation: '', phone: null, email: null, nid: null, address: null, qualification: null, joinDate: null, basicSalary: null, isActive: true, photo: null });

@Component({
  selector: 'app-staff-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, TimePipe, Avatar, Badge, Button, Card, CellDef, DataTable, EmptyState, FormField, Icon, LoadError, Modal, PageHeader, Segmented, Skeleton],
  templateUrl: './staff-page.html',
})
export class StaffPage {
  private api = inject(OfficeApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private printer = inject(PrintService);

  protected tab = signal<Tab>('list');
  protected tabs = [{ value: 'list' as Tab, label: 'উস্তাদ ও স্টাফ' }, { value: 'attendance' as Tab, label: 'হাজিরা' }, { value: 'salary' as Tab, label: 'বেতন' }];
  protected status = STATUS;
  protected statusOf = (s: AttendanceStatus | null) => STATUS.find((x) => x.value === s);
  protected photo = (p: string | null) => fileUrl(p);

  // list
  protected rows = signal<StaffRow[] | null>(null);
  protected cols: Column<StaffRow>[] = [
    { key: 'name', label: 'নাম', sortable: true, primary: true, value: (r) => `${r.name} ${r.staffCode} ${r.designation}` },
    { key: 'phone', label: 'মোবাইল' },
    { key: 'subjects', label: 'কিতাব', sortable: true, align: 'end' },
    { key: 'basicSalary', label: 'মূল বেতন', sortable: true, align: 'end' },
    { key: 'today', label: 'আজ', value: (r) => this.statusOf(r.today)?.label ?? '' },
  ];

  // detail / form
  protected open = signal(false);
  protected editId = signal<number | null>(null);
  protected detail = signal<StaffDetail | null>(null);
  protected form = signal<StaffInput>(blank());
  protected preview = signal<string | null>(null);
  protected errs = signal<Record<string, string>>({});
  protected saving = signal(false);

  // attendance
  protected date = signal(todayIso());
  protected today = todayIso();
  protected day = signal<StaffDayRow[] | null>(null);
  protected marks = signal<Record<number, AttendanceStatus | null>>({});
  protected dirty = computed(() => (this.day() ?? []).some((r) => (this.marks()[r.staffId] ?? null) !== r.status));
  protected tally = computed(() => {
    const m = this.marks(); const all = this.day() ?? [];
    return STATUS.map((s) => ({ ...s, n: all.filter((r) => m[r.staffId] === s.value).length })).concat([{ value: 'Present', label: 'বাকি', tone: 'neutral', n: all.filter((r) => !m[r.staffId]).length }]);
  });
  protected marking = signal(false);

  // salary
  protected month = signal(todayIso().slice(0, 7));
  protected sheet = signal<SalaryRow[] | null>(null);
  protected funds = signal<FundBalance[]>([]);
  protected sheetTotals = computed(() => {
    const s = this.sheet() ?? [];
    return { paid: s.filter((r) => r.paymentId).reduce((a, r) => a + r.netPaid, 0), due: s.filter((r) => !r.paymentId).reduce((a, r) => a + r.basic, 0),
      paidN: s.filter((r) => r.paymentId).length, n: s.length };
  });
  protected payOpen = signal(false);
  protected payRow = signal<SalaryRow | null>(null);
  protected pay = signal({ allowance: '0', deduction: '0', fundId: 1, paidOn: todayIso() });
  protected payNet = computed(() => { const r = this.payRow(); const p = this.pay(); return r ? r.basic + (+toEnDigits(p.allowance) || 0) - (+toEnDigits(p.deduction) || 0) : 0; });
  protected payErrs = signal<Record<string, string>>({});
  protected paying = signal(false);

  protected loadError = signal<string | null>(null);
  private failed(e: unknown) { this.loadError.set(errorMessage(e, 'তথ্য লোড করা যায়নি।')); }
  protected retry() { this.loadError.set(null); this.load(); if (this.tab() === 'attendance') this.loadDay(this.date()); if (this.tab() === 'salary') this.loadSheet(this.month()); }

  constructor() {
    this.load();
    effect(() => { const t = this.tab(), d = this.date(); if (t === 'attendance') untracked(() => this.loadDay(d)); });
    effect(() => { const t = this.tab(), m = this.month(); if (t === 'salary') untracked(() => this.loadSheet(m)); });
  }

  private async load() { try { this.rows.set(await this.api.staff()); } catch (e) { this.failed(e); } }
  private async loadDay(d: string) {
    this.day.set(null);
    try { const r = await this.api.staffDay(d); this.day.set(r); this.marks.set(Object.fromEntries(r.map((x) => [x.staffId, x.status]))); }
    catch (e) { this.failed(e); }
  }
  private async loadSheet(m: string) {
    this.sheet.set(null);
    try {
      const [s, sum] = await Promise.all([this.api.salary(m), this.api.summary(m).catch(() => null)]);
      this.sheet.set(s); if (sum) this.funds.set(sum.funds);
    } catch (e) { this.failed(e); }
  }

  protected create() { this.editId.set(null); this.detail.set(null); this.form.set(blank()); this.preview.set(null); this.errs.set({}); this.open.set(true); }
  protected async edit(id: number) {
    this.editId.set(id); this.errs.set({}); this.preview.set(null); this.open.set(true); this.detail.set(null);
    try {
      const d = await this.api.staffOne(id);
      this.detail.set(d);
      this.form.set({ name: d.name, designation: d.designation, phone: d.phone, email: d.email, nid: d.nid, address: d.address, qualification: d.qualification,
        joinDate: d.joinDate, basicSalary: d.basicSalary, isActive: d.isActive, photo: null });
    } catch (e) { this.toast.error(errorMessage(e)); this.open.set(false); }
  }
  protected patch(p: Partial<StaffInput>) { this.form.update((f) => ({ ...f, ...p })); }
  protected async pickPhoto(ev: Event) {
    const f = (ev.target as HTMLInputElement).files?.[0]; if (!f) return;
    try { const d = await resizePhoto(f); this.preview.set(d); this.patch({ photo: d }); } catch (e) { this.toast.error((e as Error).message); }
  }
  protected async save() {
    const f = this.form();
    const salary = f.basicSalary === null || String(f.basicSalary) === '' ? null : +toEnDigits(String(f.basicSalary));
    this.saving.set(true); this.errs.set({});
    try {
      const d = await this.api.saveStaff(this.editId(), { ...f, phone: f.phone ? toEnDigits(f.phone) : null, basicSalary: salary });
      this.toast.success(this.editId() ? 'তথ্য সংরক্ষণ হয়েছে' : `${d.name} (${d.staffCode}) যোগ হয়েছে`);
      this.open.set(false); this.load();
    } catch (e) { const fe = fieldErrors(e); this.errs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.saving.set(false); }
  }
  protected async remove() {
    const d = this.detail(); if (!d) return;
    if (!(await this.confirm.ask({ message: 'তালিকা থেকে সরিয়ে দেওয়া হবে। তার কিতাবগুলো উস্তাদ ছাড়া থাকবে। আগের বেতনের হিসাব থেকে যাবে।', itemName: d.name, confirmLabel: 'সরিয়ে দিন' }))) return;
    try { await this.api.deleteStaff(d.id); this.toast.success('সরিয়ে দেওয়া হয়েছে'); this.open.set(false); this.load(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }

  protected setMark(id: number, s: AttendanceStatus) { this.marks.update((m) => ({ ...m, [id]: m[id] === s ? null : s })); }
  protected allPresent() { this.marks.update((m) => Object.fromEntries((this.day() ?? []).map((r) => [r.staffId, m[r.staffId] ?? 'Present']))); }
  protected async saveMarks() {
    this.marking.set(true);
    try {
      const marks = (this.day() ?? []).map((r) => ({ staffId: r.staffId, status: this.marks()[r.staffId] ?? null }));
      const r = await this.api.markStaff(this.date(), marks);
      this.toast.success(`হাজিরা সংরক্ষণ হয়েছে (${r.changed.toLocaleString('bn-BD')}টি পরিবর্তন)`);
      this.loadDay(this.date()); if (this.date() === this.today) this.load();
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.marking.set(false); }
  }

  protected openPay(r: SalaryRow) {
    this.payRow.set(r); this.payErrs.set({});
    const general = this.funds().find((f) => f.code === 'GENERAL')?.id ?? 1;
    this.pay.set({ allowance: '0', deduction: '0', fundId: general, paidOn: todayIso() });
    this.payOpen.set(true);
  }
  protected patchPay(p: Partial<{ allowance: string; deduction: string; fundId: number; paidOn: string }>) { this.pay.update((x) => ({ ...x, ...p })); }
  protected async confirmPay() {
    const r = this.payRow(); if (!r) return; const p = this.pay();
    this.paying.set(true); this.payErrs.set({});
    try {
      const paid = await this.api.pay({ staffId: r.staffId, month: r.month, allowance: +toEnDigits(p.allowance) || 0, deduction: +toEnDigits(p.deduction) || 0, fundId: +p.fundId, paidOn: p.paidOn });
      this.toast.success(`${paid.name}-কে ${paid.netPaid.toLocaleString('bn-BD')} টাকা বেতন দেওয়া হয়েছে`);
      this.payOpen.set(false); this.printer.print({ kind: 'payslip', data: [paid] }, 'a4'); this.loadSheet(this.month());
    } catch (e) { const fe = fieldErrors(e); this.payErrs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.paying.set(false); }
  }
  protected async cancelPay(r: SalaryRow) {
    if (!r.paymentId) return;
    if (!(await this.confirm.ask({ message: 'বেতন প্রদান বাতিল হলে হিসাব খাতা থেকেও খরচটি মুছে যাবে।', itemName: `${r.name} · ${r.netPaid.toLocaleString('bn-BD')} টাকা`, confirmLabel: 'বাতিল করুন' }))) return;
    try { await this.api.cancelPay(r.paymentId); this.toast.success('বেতন প্রদান বাতিল হয়েছে'); this.loadSheet(this.month()); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected printSlips(rows: SalaryRow[]) { const paid = rows.filter((r) => r.paymentId); if (paid.length) this.printer.print({ kind: 'payslip', data: paid }, 'a4'); }
  protected monthInput(v: string) { if (v) this.month.set(v); }
  protected dateInput(v: string) { if (v) this.date.set(v); }
}
