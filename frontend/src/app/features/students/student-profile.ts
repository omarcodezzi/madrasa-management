import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { errorMessage, fileUrl } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { addMonths, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, todayIso } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Avatar, Badge, Button, Card, ConfirmService, EmptyState, Icon, Segmented, Skeleton } from '../../shared/ui';
import { StudentDues } from '../fees/fee.models';
import { IdCard } from '../idcards/id-card';
import { AttendanceCalendar } from './attendance-calendar';
import { GENDER_LABEL, STATUS_LABEL, StudentDetail, StudentMonth } from './student.models';
import { StudentsApi } from './students.service';

type Tab = 'overview' | 'attendance' | 'fees' | 'guardian' | 'academic' | 'hifz' | 'hostel' | 'documents';

@Component({
  selector: 'app-student-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, Avatar, Badge, Button, Card, EmptyState, Icon, Segmented, Skeleton, IdCard, AttendanceCalendar],
  templateUrl: './student-profile.html',
})
export class StudentProfilePage {
  readonly id = input.required<string>();
  private api = inject(StudentsApi);
  private router = inject(Router);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private printer = inject(PrintService);
  private auth = inject(AuthService);

  protected s = signal<StudentDetail | null>(null);
  protected att = signal<StudentMonth | null>(null);
  protected dues = signal<StudentDues | null>(null);
  protected error = signal<string | null>(null);
  protected month = signal(todayIso().slice(0, 7));
  protected today = todayIso();
  protected tab = signal<Tab>('overview');
  protected genderLabel = GENDER_LABEL;
  protected statusLabel = STATUS_LABEL;
  protected canEdit = computed(() => this.auth.can(P.studentsEdit));
  protected canFees = computed(() => this.auth.can(P.fees));
  protected photo = computed(() => fileUrl(this.s()?.photoPath));
  protected primary = computed(() => this.s()?.guardians.find((g) => g.isPrimary) ?? this.s()?.guardians[0]);
  protected tabs = computed(() => [
    { value: 'overview' as Tab, label: 'সারসংক্ষেপ' },
    { value: 'attendance' as Tab, label: 'হাজিরা' },
    ...(this.canFees() ? [{ value: 'fees' as Tab, label: 'বেতন' }] : []),
    { value: 'guardian' as Tab, label: 'অভিভাবক' },
    { value: 'academic' as Tab, label: 'ফলাফল' },
    { value: 'hifz' as Tab, label: 'হিফজ' },
    { value: 'hostel' as Tab, label: 'হোস্টেল' },
    { value: 'documents' as Tab, label: 'কাগজপত্র' },
  ]);
  protected later: Partial<Record<Tab, { phase: number; text: string }>> = {
    academic: { phase: 5, text: 'পরীক্ষার নম্বর, বিভাজন ও মার্কশিট এখানে দেখাবে।' },
    hifz: { phase: 5, text: 'সবক, সবকি, মানযিল ও ৩০ পারার অগ্রগতি এখানে দেখাবে।' },
    hostel: { phase: 6, text: 'রুম, সিট ও ছুটির রেকর্ড এখানে দেখাবে।' },
    documents: { phase: 6, text: 'জন্ম নিবন্ধন, ছাড়পত্র ইত্যাদি কাগজ এখানে রাখা যাবে।' },
  };

  constructor() {
    effect(() => { const id = +this.id(); untracked(() => this.load(id)); });
    effect(() => { const m = this.month(); const s = this.s(); if (s) untracked(() => this.api.attendance(s.id, m).then((a) => this.att.set(a)).catch(() => {})); });
  }

  async load(id: number) {
    this.error.set(null);
    this.s.set(null);
    try {
      this.s.set(await this.api.get(id));
      if (this.canFees()) this.api.dues(id).then((d) => this.dues.set(d)).catch(() => {});
    } catch (e) { this.error.set(errorMessage(e)); }
  }
  protected moveMonth(n: number) { this.month.update((m) => addMonths(m, n)); }
  protected printCard() { const s = this.s(); if (s) this.printer.print({ kind: 'idcards', data: [s] }); }
  protected printForm() { const s = this.s(); if (s) this.printer.print({ kind: 'admission', data: s }); }
  protected async remove() {
    const s = this.s();
    if (!s || !(await this.confirm.ask({ message: 'শিক্ষার্থীকে তালিকা থেকে সরানো হবে। পুরনো হাজিরা ও বেতনের হিসাব রিপোর্টে থেকে যাবে।', itemName: s.name, confirmLabel: 'সরিয়ে দিন' }))) return;
    try { await this.api.remove(s.id); this.toast.success(`${s.name} কে সরানো হয়েছে`); this.router.navigate(['/students']); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
}
