import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { errorMessage, fileUrl } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { addMonths, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, todayIso } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Avatar, Badge, Button, Card, ConfirmService, EmptyState, Icon, Segmented, Skeleton } from '../../shared/ui';
import { StudentDues } from '../fees/fee.models';
import { ExamsApi } from '../exams/exams.service';
import { StudentExamResult } from '../exams/exam.models';
import { HifzApi } from '../hifz/hifz.service';
import { HifzStudentDetail, QUALITY_LABEL } from '../hifz/hifz.models';
import { JuzGrid, JuzRing } from '../hifz/juz-grid';
import { IdCard } from '../idcards/id-card';
import { AttendanceCalendar } from './attendance-calendar';
import { GENDER_LABEL, STATUS_LABEL, StudentDetail, StudentMonth } from './student.models';
import { StudentsApi } from './students.service';
import { TrPipe, tr } from '../../core/tr';

type Tab = 'overview' | 'attendance' | 'fees' | 'guardian' | 'academic' | 'hifz' | 'hostel' | 'documents';

@Component({
  selector: 'app-student-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, RouterLink, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, Avatar, Badge, Button, Card, EmptyState, Icon, Segmented, Skeleton, IdCard, AttendanceCalendar, JuzGrid, JuzRing],
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
  private examsApi = inject(ExamsApi);
  private hifzApi = inject(HifzApi);
  protected results = signal<StudentExamResult[] | null>(null);
  protected hifz = signal<HifzStudentDetail | null>(null);
  protected qLabel = QUALITY_LABEL;
  protected canHifz = computed(() => this.auth.can(P.hifz));

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
    { value: 'overview' as Tab, label: tr('সারসংক্ষেপ') },
    { value: 'attendance' as Tab, label: tr('হাজিরা') },
    ...(this.canFees() ? [{ value: 'fees' as Tab, label: tr('বেতন') }] : []),
    { value: 'guardian' as Tab, label: tr('অভিভাবক') },
    { value: 'academic' as Tab, label: tr('ফলাফল') },
    { value: 'hifz' as Tab, label: tr('হিফজ') },
    { value: 'hostel' as Tab, label: tr('হোস্টেল') },
    { value: 'documents' as Tab, label: tr('কাগজপত্র') },
  ]);
  protected later: Partial<Record<Tab, { phase: number; text: string }>> = {
    hostel: { phase: 6, text: tr('রুম, সিট ও ছুটির রেকর্ড এখানে দেখাবে।') },
    documents: { phase: 6, text: tr('জন্ম নিবন্ধন, ছাড়পত্র ইত্যাদি কাগজ এখানে রাখা যাবে।') },
  };

  constructor() {
    effect(() => { const id = +this.id(); untracked(() => this.load(id)); });
    // Results and hifz load when their tab is first opened
    effect(() => {
      const t = this.tab(), s = this.s();
      if (!s) return;
      untracked(() => {
        if (t === 'academic' && !this.results()) this.examsApi.student(s.id).then((r) => this.results.set(r)).catch(() => this.results.set([]));
        if (t === 'hifz' && !this.hifz()) this.hifzApi.student(s.id).then((h) => this.hifz.set(h)).catch(() => {});
      });
    });
    effect(() => { const m = this.month(); const s = this.s(); if (s) untracked(() => this.api.attendance(s.id, m).then((a) => this.att.set(a)).catch(() => {})); });
  }

  async load(id: number) {
    this.error.set(null);
    this.s.set(null); this.results.set(null); this.hifz.set(null);
    try {
      this.s.set(await this.api.get(id));
      if (this.canFees()) this.api.dues(id).then((d) => this.dues.set(d)).catch(() => {});
    } catch (e) { this.error.set(errorMessage(e)); }
  }
  protected moveMonth(n: number) { this.month.update((m) => addMonths(m, n)); }
  protected printCard() { const s = this.s(); if (s) this.printer.print({ kind: 'idcards', data: [s] }); }
  protected printMarkSheet(r: StudentExamResult) { const s = this.s(); if (s) this.printer.print({ kind: 'marksheet', data: [{ student: s, result: r }] }); }
  protected failed = (x: { marks?: number | null; isAbsent: boolean; passMarks: number }) => x.isAbsent || (x.marks ?? 0) < x.passMarks;
  protected printForm() { const s = this.s(); if (s) this.printer.print({ kind: 'admission', data: s }); }
  protected async remove() {
    const s = this.s();
    if (!s || !(await this.confirm.ask({ message: tr('শিক্ষার্থীকে তালিকা থেকে সরানো হবে। পুরনো হাজিরা ও বেতনের হিসাব রিপোর্টে থেকে যাবে।'), itemName: s.name, confirmLabel: tr('সরিয়ে দিন') }))) return;
    try { await this.api.remove(s.id); this.toast.success(tr('{name} কে সরানো হয়েছে', { name: s.name })); this.router.navigate(['/students']); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
}
