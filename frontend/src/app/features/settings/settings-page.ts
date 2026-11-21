import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { errorMessage, fileUrl } from '../../core/api';
import { AuthService } from '../../core/auth/auth.service';
import { DatePipe, DigitsPipe, MoneyPipe, NumPipe, toEnDigits, todayIso } from '../../core/format';
import { SessionService } from '../../core/session.service';
import { ToastService } from '../../core/toast.service';
import { Badge, Button, Card, ConfirmService, EmptyState, FormField, Icon, IconName, Modal, PageHeader, Skeleton, resizePhoto } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { FeeFrequency, FeeHeadRow, FREQ_LABEL, GeneralSettings, GradeRow, Holiday, Profile, RoleRow, Session, UserInput, UserRow } from '../office/office.models';
import { Guardian, StudentRow } from '../students/student.models';
import { StudentsApi } from '../students/students.service';

type Section = 'profile' | 'sessions' | 'users' | 'roles' | 'fees' | 'grades' | 'holidays' | 'general';
const SECTIONS: { key: Section; label: string; icon: IconName }[] = [
  { key: 'profile', label: 'মাদরাসার তথ্য', icon: 'home' }, { key: 'sessions', label: 'শিক্ষাবর্ষ', icon: 'calendar' },
  { key: 'users', label: 'ইউজার', icon: 'users' }, { key: 'roles', label: 'রোল ও অনুমতি', icon: 'user' },
  { key: 'fees', label: 'ফি-এর খাত', icon: 'wallet' }, { key: 'grades', label: 'বিভাগ (গ্রেড)', icon: 'exam' },
  { key: 'holidays', label: 'ছুটির দিন', icon: 'sun' }, { key: 'general', label: 'সাধারণ', icon: 'gear' },
];
export const PERM_LABEL: Record<string, string> = {
  'dashboard.view': 'ড্যাশবোর্ড দেখা', 'students.view': 'শিক্ষার্থী দেখা', 'students.edit': 'শিক্ষার্থীর তথ্য বদলানো', 'admission.manage': 'ভর্তি',
  'attendance.take': 'হাজিরা নেওয়া', 'hifz.edit': 'হিফজের সবক লেখা', 'exams.marks': 'নম্বর এন্ট্রি', 'exams.publish': 'ফলাফল প্রকাশ', 'fees.collect': 'বেতন আদায়',
  'accounts.manage': 'হিসাব ও দান', 'staff.manage': 'স্টাফ ও বেতন', 'hostel.manage': 'হোস্টেল', 'library.manage': 'লাইব্রেরি', 'notices.manage': 'নোটিশ দেওয়া',
  'reports.view': 'রিপোর্ট দেখা', 'settings.manage': 'সেটিংস', 'portal.view': 'অভিভাবক পোর্টাল',
};
export const ROLE_LABEL: Record<string, string> = {
  SuperAdmin: 'সুপার অ্যাডমিন', Muhtamim: 'মুহতামিম', Office: 'অফিস', Teacher: 'উস্তাদ', Accountant: 'হিসাবরক্ষক', HostelSupervisor: 'হোস্টেল তত্ত্বাবধায়ক',
  Librarian: 'লাইব্রেরিয়ান', Student: 'শিক্ষার্থী', Guardian: 'অভিভাবক',
};
const DAYS = ['রবিবার', 'সোমবার', 'মঙ্গলবার', 'বুধবার', 'বৃহস্পতিবার', 'শুক্রবার', 'শনিবার'];

@Component({
  selector: 'app-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, DigitsPipe, MoneyPipe, NumPipe, Badge, Button, Card, EmptyState, FormField, Icon, Modal, PageHeader, Skeleton],
  templateUrl: './settings-page.html',
})
export class SettingsPage {
  private api = inject(OfficeApi);
  private students = inject(StudentsApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private session = inject(SessionService);
  private auth = inject(AuthService);
  private router = inject(Router);

  protected sections = SECTIONS;
  protected section = signal<Section>((inject(ActivatedRoute).snapshot.queryParamMap.get('s') as Section) || 'profile');
  protected days = DAYS.map((label, value) => ({ value, label }));
  protected permLabel = PERM_LABEL;
  protected allPerms = Object.keys(PERM_LABEL);
  protected roleLabel = (r: string) => ROLE_LABEL[r] ?? r;
  protected freqs = (Object.keys(FREQ_LABEL) as FeeFrequency[]).map((f) => ({ value: f, label: FREQ_LABEL[f] }));
  protected fLabel = (f: string) => (FREQ_LABEL as Record<string, string>)[f] ?? f;
  protected errs = signal<Record<string, string>>({});
  protected saving = signal(false);

  // data per section
  protected profile = signal<Profile | null>(null);
  protected logo = signal<string | null>(null);
  protected sessions = signal<Session[] | null>(null);
  protected users = signal<UserRow[] | null>(null);
  protected roles = signal<RoleRow[] | null>(null);
  protected heads = signal<FeeHeadRow[] | null>(null);
  protected grades = signal<{ nameBn: string; nameAr: string; minAverage: string }[] | null>(null);
  protected year = signal(new Date().getFullYear());
  protected holidays = signal<Holiday[] | null>(null);
  protected general = signal<GeneralSettings | null>(null);
  protected me = computed(() => this.auth.user()?.id);

  constructor() {
    effect(() => { const s = this.section(); untracked(() => this.load(s)); });
    effect(() => { const y = this.year(); if (this.section() === 'holidays') untracked(() => this.api.holidays(y).then((h) => this.holidays.set(h)).catch(() => {})); });
  }
  protected go(s: Section) { this.section.set(s); this.errs.set({}); this.router.navigate([], { queryParams: { s }, replaceUrl: true }); }
  private async load(s: Section) {
    try {
      switch (s) {
        case 'profile': this.profile.set(await this.api.profile()); this.logo.set(null); break;
        case 'sessions': this.sessions.set(await this.api.sessions()); break;
        case 'users': this.users.set(await this.api.users()); if (!this.roles()) this.roles.set(await this.api.roles()); break;
        case 'roles': this.roles.set(await this.api.roles()); break;
        case 'fees': this.heads.set(await this.api.feeHeads()); break;
        case 'grades': this.grades.set((await this.api.grades()).map((g) => ({ nameBn: g.nameBn, nameAr: g.nameAr ?? '', minAverage: String(g.minAverage) }))); break;
        case 'holidays': this.holidays.set(await this.api.holidays(this.year())); break;
        case 'general': this.general.set(await this.api.general()); break;
      }
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
  private async run(fn: () => Promise<unknown>, ok: string) {
    this.saving.set(true); this.errs.set({});
    try { await fn(); this.toast.success(ok); return true; }
    catch (e) { const fe = fieldErrors(e); this.errs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); return false; }
    finally { this.saving.set(false); }
  }

  // ---- profile ----
  protected logoUrl = computed(() => this.logo() ?? fileUrl(this.profile()?.logoPath) ?? '/logo.jpg');
  protected patchProfile(p: Partial<Profile>) { this.profile.update((x) => (x ? { ...x, ...p } : x)); }
  protected async pickLogo(ev: Event) {
    const f = (ev.target as HTMLInputElement).files?.[0]; if (!f) return;
    try { this.logo.set(await resizePhoto(f, 240, 240)); } catch (e) { this.toast.error((e as Error).message); }
  }
  protected saveProfile() {
    const p = this.profile(); if (!p) return;
    this.run(async () => {
      const r = await this.api.saveProfile({ ...p, establishedYear: p.establishedYear ? +toEnDigits(String(p.establishedYear)) : null, logo: this.logo() });
      this.profile.set(r); this.logo.set(null); this.session.reload();
    }, 'মাদরাসার তথ্য সংরক্ষণ হয়েছে');
  }

  // ---- sessions ----
  protected sOpen = signal(false);
  protected sId = signal<number | null>(null);
  protected sForm = signal({ name: '', startDate: '', endDate: '' });
  protected newSession() {
    const y = new Date().getFullYear() + 1;
    this.sId.set(null); this.errs.set({}); this.sForm.set({ name: String(y), startDate: `${y}-01-01`, endDate: `${y}-12-31` }); this.sOpen.set(true);
  }
  protected editSession(s: Session) { this.sId.set(s.id); this.errs.set({}); this.sForm.set({ name: s.name, startDate: s.startDate, endDate: s.endDate }); this.sOpen.set(true); }
  protected patchS(p: Partial<{ name: string; startDate: string; endDate: string }>) { this.sForm.update((f) => ({ ...f, ...p })); }
  protected async saveSession() { if (await this.run(() => this.api.saveSession(this.sId(), this.sForm()), 'শিক্ষাবর্ষ সংরক্ষণ হয়েছে')) { this.sOpen.set(false); this.load('sessions'); } }
  protected async makeCurrent(s: Session) {
    if (!(await this.confirm.ask({ title: 'চলতি শিক্ষাবর্ষ বদলাবেন?', message: 'হাজিরা, বেতন, পরীক্ষা সব এই শিক্ষাবর্ষ ধরে চলবে। শিক্ষার্থীদের নতুন শ্রেণিতে তোলা আলাদা কাজ, এতে হবে না।', itemName: s.name, confirmLabel: 'চলতি করুন' }))) return;
    if (await this.run(() => this.api.setCurrentSession(s.id), `${s.name} এখন চলতি শিক্ষাবর্ষ`)) { this.load('sessions'); this.session.reload(); }
  }

  // ---- users ----
  protected uOpen = signal(false);
  protected uId = signal<string | null>(null);
  protected uForm = signal<UserInput>({ userName: '', fullName: '', password: '', roles: [], isActive: true, staffId: null, studentId: null, guardianId: null });
  protected staffList = signal<{ id: number; name: string; designation: string }[]>([]);
  protected portalRole = computed(() => this.uForm().roles.some((r) => r === 'Student' || r === 'Guardian'));
  protected studentQ = signal('');
  protected studentHits = signal<StudentRow[]>([]);
  protected linkedStudent = signal<{ id: number; name: string; guardians: Guardian[] } | null>(null);
  protected pwOpen = signal(false);
  protected pwUser = signal<UserRow | null>(null);
  protected pw = signal('');

  protected async newUser() {
    this.uId.set(null); this.errs.set({}); this.linkedStudent.set(null); this.studentQ.set(''); this.studentHits.set([]);
    this.uForm.set({ userName: '', fullName: '', password: '', roles: ['Teacher'], isActive: true, staffId: null, studentId: null, guardianId: null });
    this.uOpen.set(true); this.loadStaff();
  }
  protected editUser(u: UserRow) {
    this.uId.set(u.id); this.errs.set({}); this.linkedStudent.set(null); this.studentQ.set('');
    this.uForm.set({ userName: u.userName, fullName: u.fullName, password: '', roles: [...u.roles], isActive: u.isActive, staffId: u.staffId, studentId: u.studentId, guardianId: u.guardianId });
    this.uOpen.set(true); this.loadStaff();
  }
  private loadStaff() { if (!this.staffList().length) this.api.staff().then((s) => this.staffList.set(s.filter((x) => x.isActive))).catch(() => {}); }
  protected patchU(p: Partial<UserInput>) { this.uForm.update((f) => ({ ...f, ...p })); }
  protected toggleRole(r: string) {
    const portal = r === 'Student' || r === 'Guardian';
    this.uForm.update((f) => {
      const has = f.roles.includes(r);
      const roles = has ? f.roles.filter((x) => x !== r) : portal ? [r] : [...f.roles.filter((x) => x !== 'Student' && x !== 'Guardian'), r];
      return { ...f, roles, ...(portal && !has ? { staffId: null } : {}), ...(!portal && !has ? { studentId: null, guardianId: null } : {}) };
    });
  }
  protected async searchStudents(q: string) {
    this.studentQ.set(q);
    if (q.trim().length < 2) { this.studentHits.set([]); return; }
    try { this.studentHits.set((await this.students.all({ q: toEnDigits(q.trim()), pageSize: 8 })).items); } catch { this.studentHits.set([]); }
  }
  protected async linkStudent(s: StudentRow) {
    this.studentHits.set([]); this.studentQ.set('');
    try {
      const d = await this.students.get(s.id);
      this.linkedStudent.set({ id: d.id, name: d.name, guardians: d.guardians });
      if (this.uForm().roles.includes('Student')) this.patchU({ studentId: d.id, guardianId: null, fullName: this.uForm().fullName || d.name });
      else { const g = d.guardians.find((x) => x.isPrimary) ?? d.guardians[0]; this.patchU({ guardianId: g?.id ?? null, studentId: null, fullName: this.uForm().fullName || (g?.name ?? '') }); }
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected async saveUser() {
    const f = this.uForm();
    if (await this.run(() => this.api.saveUser(this.uId(), { ...f, password: f.password || null }), this.uId() ? 'ইউজার সংরক্ষণ হয়েছে' : `ইউজার "${f.userName}" তৈরি হয়েছে`)) { this.uOpen.set(false); this.load('users'); }
  }
  protected resetPw(u: UserRow) { this.pwUser.set(u); this.pw.set(''); this.errs.set({}); this.pwOpen.set(true); }
  protected async savePw() {
    const u = this.pwUser(); if (!u) return;
    if (await this.run(() => this.api.resetPassword(u.id, this.pw()), `${u.fullName}-এর নতুন পাসওয়ার্ড সেট হয়েছে`)) { this.pwOpen.set(false); this.load('users'); }
  }

  // ---- roles ----
  protected roleEdit = signal<Record<string, string[]>>({});
  protected rolePerms = (r: RoleRow) => this.roleEdit()[r.name] ?? r.permissions;
  protected roleDirty = (r: RoleRow) => !!this.roleEdit()[r.name];
  protected portalOnly = (r: RoleRow) => r.name === 'Student' || r.name === 'Guardian';
  protected togglePerm(r: RoleRow, p: string) {
    const cur = this.rolePerms(r);
    this.roleEdit.update((m) => ({ ...m, [r.name]: cur.includes(p) ? cur.filter((x) => x !== p) : [...cur, p] }));
  }
  protected async saveRole(r: RoleRow) {
    const perms = this.roleEdit()[r.name]; if (!perms) return;
    if (await this.run(async () => { const n = await this.api.setRole(r.name, perms); this.roles.update((l) => l?.map((x) => (x.name === n.name ? n : x)) ?? l); },
      `${this.roleLabel(r.name)} রোলের অনুমতি সংরক্ষণ হয়েছে। ব্যবহারকারীরা আবার লগইন করলে কার্যকর হবে।`)) {
      this.roleEdit.update((m) => { const n = { ...m }; delete n[r.name]; return n; });
    }
  }

  // ---- fee heads ----
  protected hOpen = signal(false);
  protected hId = signal<number | null>(null);
  protected hForm = signal({ nameBn: '', frequency: 'Monthly' as FeeFrequency, defaultAmount: '', isActive: true });
  protected newHead() { this.hId.set(null); this.errs.set({}); this.hForm.set({ nameBn: '', frequency: 'Once', defaultAmount: '', isActive: true }); this.hOpen.set(true); }
  protected editHead(h: FeeHeadRow) { this.hId.set(h.id); this.errs.set({}); this.hForm.set({ nameBn: h.nameBn, frequency: h.frequency, defaultAmount: String(h.defaultAmount), isActive: h.isActive }); this.hOpen.set(true); }
  protected patchH(p: Partial<{ nameBn: string; frequency: FeeFrequency; defaultAmount: string; isActive: boolean }>) { this.hForm.update((f) => ({ ...f, ...p })); }
  protected async saveHead() {
    const f = this.hForm();
    if (await this.run(() => this.api.saveFeeHead(this.hId(), { ...f, defaultAmount: +toEnDigits(f.defaultAmount) || 0 }), 'ফি-এর খাত সংরক্ষণ হয়েছে')) { this.hOpen.set(false); this.load('fees'); }
  }

  // ---- grades ----
  protected patchGrade(i: number, p: Partial<{ nameBn: string; nameAr: string; minAverage: string }>) { this.grades.update((g) => g?.map((x, j) => (j === i ? { ...x, ...p } : x)) ?? g); }
  protected addGrade() { this.grades.update((g) => [...(g ?? []), { nameBn: '', nameAr: '', minAverage: '' }]); }
  protected removeGrade(i: number) { this.grades.update((g) => g?.filter((_, j) => j !== i) ?? g); }
  protected async saveGrades() {
    const g = this.grades() ?? [];
    if (await this.run(async () => {
      const r = await this.api.saveGrades(g.map((x) => ({ nameBn: x.nameBn, nameAr: x.nameAr || null, minAverage: +toEnDigits(x.minAverage) })));
      this.grades.set(r.map((x: GradeRow) => ({ nameBn: x.nameBn, nameAr: x.nameAr ?? '', minAverage: String(x.minAverage) })));
    }, 'বিভাগ সংরক্ষণ হয়েছে। পরের বার ফলাফল তৈরিতে নতুন নিয়ম লাগবে।')) { /* saved */ }
  }

  // ---- holidays ----
  protected hdForm = signal({ date: todayIso(), title: '' });
  protected async addHoliday() {
    const f = this.hdForm();
    if (await this.run(() => this.api.addHoliday(f), 'ছুটি যোগ হয়েছে')) { this.hdForm.set({ date: f.date, title: '' }); this.api.holidays(this.year()).then((h) => this.holidays.set(h)); }
  }
  protected async removeHoliday(h: Holiday) {
    if (!(await this.confirm.ask({ message: 'এই দিনটি আবার কর্মদিবস হিসেবে গণ্য হবে।', itemName: h.title, confirmLabel: 'মুছে ফেলুন' }))) return;
    if (await this.run(() => this.api.deleteHoliday(h.id), 'ছুটি মুছে ফেলা হয়েছে')) this.api.holidays(this.year()).then((x) => this.holidays.set(x));
  }

  // ---- general ----
  protected patchG(p: Partial<GeneralSettings>) { this.general.update((g) => (g ? { ...g, ...p } : g)); }
  protected saveGeneral() {
    const g = this.general(); if (!g) return;
    this.run(async () => this.general.set(await this.api.saveGeneral({ ...g, libraryFinePerDay: +toEnDigits(String(g.libraryFinePerDay)) || 0, weeklyHoliday: +g.weeklyHoliday })), 'সংরক্ষণ হয়েছে');
  }
}
