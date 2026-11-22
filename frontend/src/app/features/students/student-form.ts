import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { errorMessage, fileUrl, Gender } from '../../core/api';
import { DatePipe, DigitsPipe, MoneyPipe, NumPipe, toEnDigits, todayIso } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { ToastService } from '../../core/toast.service';
import { HasUnsavedChanges } from '../../core/unsaved.guard';
import { PrintService } from '../../shared/print/print.service';
import { Avatar, Button, Card, FormField, Icon, PageHeader, resizePhoto, Skeleton } from '../../shared/ui';
import { Guardian, StudentDetail, StudentInput, GENDER_LABEL } from './student.models';
import { StudentsApi } from './students.service';
import { OfficeApi } from '../office/office.api';
import { TrPipe, tr } from '../../core/tr';

const DRAFT_KEY = 'mms.admission-draft';
const STEPS = [tr('শিক্ষার্থীর তথ্য'), tr('অভিভাবক ও ঠিকানা'), tr('শ্রেণি ও বেতন'), tr('যাচাই করুন')] as const;

// The relation is stored in Bangla and translated only on screen.
const blankGuardian = (relation = 'পিতা', isPrimary = true): Guardian => ({ name: '', relation, phone: '', isPrimary }); // i18n-ignore
const blank = (): StudentInput => ({
  name: '', gender: 'Male', isOrphan: false, isResidential: false, classId: null, roll: null, admissionDate: todayIso(),
  guardians: [blankGuardian()], fatherName: '', motherName: '', phone: '', presentAddress: '', permanentAddress: '',
});

/** Admission and edit: a 4-step form with per-step checks, a saved draft for new admissions, and a leave warning. */
@Component({
  selector: 'app-student-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, FormsModule, RouterLink, DigitsPipe, NumPipe, MoneyPipe, DatePipe, Avatar, Button, Card, FormField, Icon, PageHeader, Skeleton],
  templateUrl: './student-form.html',
})
export class StudentFormPage implements HasUnsavedChanges {
  /** Route param for edit (students/:id/edit) */
  readonly id = input<string>();
  /** ?application=12: admitting an online/office application, prefilled from it */
  readonly application = input<string>();
  private office = inject(OfficeApi);
  private api = inject(StudentsApi);
  private router = inject(Router);
  private toast = inject(ToastService);
  private print = inject(PrintService);
  protected lookups = inject(LookupsService);

  protected steps = STEPS;
  protected step = signal(0);
  protected f = signal<StudentInput>(blank());
  protected photo = signal<string | null>(null);
  protected existing = signal<StudentDetail | null>(null);
  protected suggested = signal<{ studentCode: string; roll: number } | null>(null);
  protected loading = signal(false);
  protected saving = signal(false);
  protected dirty = signal(false);
  protected tried = signal<Set<number>>(new Set());
  protected serverErrors = signal<Record<string, string>>({});
  protected sameAddress = signal(false);
  protected isEdit = computed(() => !!this.id());
  protected genders: { v: Gender; l: string }[] = [{ v: 'Male', l: GENDER_LABEL.Male }, { v: 'Female', l: GENDER_LABEL.Female }];
  protected relations = ['পিতা', 'মাতা', 'চাচা', 'মামা', 'ভাই', 'দাদা', 'নানা', 'অন্যান্য']; // i18n-ignore
  protected bloodGroups = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];
  protected selectedClass = computed(() => this.lookups.classes().find((c) => c.id === this.f().classId));

  constructor() {
    effect(() => {
      const id = this.id();
      const app = this.application();
      untracked(() => (id ? this.loadExisting(+id) : app ? this.loadApplication(+app) : this.loadDraft()));
    });
    // Suggest the next roll when the class changes on a new admission
    effect(() => {
      const cls = this.f().classId;
      if (this.isEdit()) return;
      untracked(() => this.api.nextCodes(cls).then((s) => this.suggested.set(s)).catch(() => {}));
    });
    // Autosave the draft of a new admission (spec 7: autosave long forms)
    effect(() => {
      const v = this.f();
      if (!this.isEdit() && !this.application() && this.dirty()) try { localStorage.setItem(DRAFT_KEY, JSON.stringify(v)); } catch { /* full */ }
    });
  }

  hasUnsavedChanges() { return this.dirty() && !this.saving(); }

  private async loadExisting(id: number) {
    this.loading.set(true);
    try {
      const s = await this.api.get(id);
      this.existing.set(s);
      this.f.set({
        name: s.name, nameAr: s.nameAr, gender: s.gender, dateOfBirth: s.dateOfBirth, birthRegNo: s.birthRegNo, bloodGroup: s.bloodGroup,
        fatherName: s.fatherName, motherName: s.motherName, phone: s.phone, presentAddress: s.presentAddress, permanentAddress: s.permanentAddress,
        admissionDate: s.admissionDate, isOrphan: s.isOrphan, isResidential: s.isResidential, status: s.status, classId: s.classId ?? null,
        sectionId: s.sectionId, roll: s.roll, monthlyFeeOverride: s.monthlyFeeOverride,
        guardians: s.guardians.length ? s.guardians.map((g) => ({ ...g })) : [blankGuardian()],
      });
      this.photo.set(fileUrl(s.photoPath));
    } catch (e) { this.toast.error(errorMessage(e)); this.router.navigate(['/students']); }
    finally { this.loading.set(false); }
  }
  protected fromApplication = signal<string | null>(null);
  private async loadApplication(appId: number) {
    try {
      const a = (await this.office.applications()).find((x) => x.id === appId);
      if (!a) { this.loadDraft(); return; }
      this.fromApplication.set(a.applicationNo);
      this.f.set({ ...blank(), name: a.applicantName, gender: a.gender, dateOfBirth: a.dateOfBirth, fatherName: a.fatherName ?? '', classId: a.classId,
        presentAddress: a.address ?? '', guardians: [{ ...blankGuardian(), name: a.fatherName ?? '', phone: a.guardianPhone }] } as StudentInput);
    } catch { this.loadDraft(); }
  }
  private loadDraft() {
    try {
      const d = JSON.parse(localStorage.getItem(DRAFT_KEY) || 'null') as StudentInput | null;
      if (d?.name || d?.guardians?.[0]?.phone) { this.f.set({ ...blank(), ...d, photoDataUrl: null }); this.hadDraft.set(true); }
    } catch { /* ignore */ }
  }
  protected hadDraft = signal(false);
  protected toEn = (v: unknown) => toEnDigits(String(v ?? ''));
  protected discardDraft() {
    try { localStorage.removeItem(DRAFT_KEY); } catch { /* ignore */ }
    this.f.set(blank()); this.hadDraft.set(false); this.dirty.set(false); this.tried.set(new Set()); this.step.set(0);
  }

  protected set<K extends keyof StudentInput>(k: K, v: StudentInput[K]) {
    this.f.update((x) => ({ ...x, [k]: v }));
    this.dirty.set(true);
    if (this.serverErrors()[k as string]) this.serverErrors.update((e) => { const c = { ...e }; delete c[k as string]; return c; });
  }
  protected setG(i: number, k: keyof Guardian, v: string | boolean) {
    this.f.update((x) => ({ ...x, guardians: x.guardians.map((g, j) => (j === i ? { ...g, [k]: v } : k === 'isPrimary' && v ? { ...g, isPrimary: false } : g)) }));
    this.dirty.set(true);
  }
  protected addGuardian() { this.set('guardians', [...this.f().guardians, blankGuardian(tr('মাতা'), false)]); }
  protected removeGuardian(i: number) {
    const l = this.f().guardians.filter((_, j) => j !== i);
    if (l.length && !l.some((g) => g.isPrimary)) l[0] = { ...l[0], isPrimary: true };
    this.set('guardians', l);
  }
  /** Copy the father's name into the first guardian when it is still empty. */
  protected fatherChanged(v: string) {
    this.set('fatherName', v);
    const g = this.f().guardians[0];
    if (g && g.relation === 'পিতা' && (!g.name || g.name === this.prevFather)) this.setG(0, 'name', v); // i18n-ignore
    this.prevFather = v;
  }
  private prevFather = '';

  protected async onPhoto(ev: Event) {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    try { const url = await resizePhoto(file); this.photo.set(url); this.set('photoDataUrl', url); }
    catch (e) { this.toast.error((e as Error).message); }
  }

  // ---------- validation (Bangla, per step) ----------
  private phoneOk = (p?: string | null) => { const d = toEnDigits(p ?? '').replace(/\D/g, '').replace(/^88/, ''); return /^01[3-9]\d{8}$/.test(d); };
  protected errors = computed(() => {
    const v = this.f(), e: Record<string, string> = { ...this.serverErrors() };
    if (!v.name.trim()) e['name'] ??= tr('শিক্ষার্থীর নাম লিখুন');
    if (v.dateOfBirth && v.dateOfBirth > todayIso()) e['dateOfBirth'] ??= tr('জন্ম তারিখ ভবিষ্যতে হতে পারে না');
    if (v.phone && !this.phoneOk(v.phone)) e['phone'] ??= tr('সঠিক মোবাইল নম্বর লিখুন (১১ সংখ্যা)');
    if (!v.fatherName?.trim()) e['fatherName'] ??= tr('পিতার নাম লিখুন');
    if (!v.guardians.length) e['guardians'] ??= tr('অন্তত একজন অভিভাবক যোগ করুন');
    v.guardians.forEach((g, i) => {
      if (!g.name.trim()) e[`guardians[${i}].name`] ??= tr('অভিভাবকের নাম লিখুন');
      if (!this.phoneOk(g.phone)) e[`guardians[${i}].phone`] ??= tr('সঠিক মোবাইল নম্বর লিখুন (১১ সংখ্যা)');
    });
    if (!v.classId) e['classId'] ??= tr('শ্রেণি বাছাই করুন');
    if (v.monthlyFeeOverride != null && v.monthlyFeeOverride < 0) e['monthlyFeeOverride'] ??= tr('বেতন ঋণাত্মক হতে পারে না');
    return e;
  });
  private stepOf(key: string) {
    if (['name', 'nameAr', 'gender', 'dateOfBirth', 'birthRegNo', 'bloodGroup', 'photo'].includes(key)) return 0;
    if (['fatherName', 'motherName', 'phone', 'presentAddress', 'permanentAddress', 'guardians'].includes(key) || key.startsWith('guardians[')) return 1;
    return 2;
  }
  protected err(key: string) { return this.tried().has(this.stepOf(key)) ? (this.errors()[key] ?? null) : null; }
  private stepValid(i: number) { return !Object.keys(this.errors()).some((k) => this.stepOf(k) === i); }

  protected next() {
    this.tried.update((s) => new Set(s).add(this.step()));
    if (!this.stepValid(this.step())) { this.toast.error(tr('লাল চিহ্নিত ঘরগুলো ঠিক করুন')); this.focusFirstError(); return; }
    this.step.update((s) => Math.min(s + 1, STEPS.length - 1));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  protected back() { this.step.update((s) => Math.max(0, s - 1)); }
  protected goTo(i: number) {
    if (i <= this.step()) { this.step.set(i); return; }
    for (let s = this.step(); s < i; s++) { this.tried.update((t) => new Set(t).add(s)); if (!this.stepValid(s)) { this.step.set(s); return; } }
    this.step.set(i);
  }
  private focusFirstError() { setTimeout(() => (document.querySelector('[aria-invalid="true"]') as HTMLElement | null)?.focus(), 0); }

  protected async save(printAfter = false) {
    this.tried.set(new Set([0, 1, 2]));
    const bad = [0, 1, 2].find((s) => !this.stepValid(s));
    if (bad !== undefined) { this.step.set(bad); this.toast.error(tr('লাল চিহ্নিত ঘরগুলো ঠিক করুন')); this.focusFirstError(); return; }
    this.saving.set(true);
    const v = this.f();
    const payload: StudentInput = {
      ...v, roll: v.roll || null, monthlyFeeOverride: v.monthlyFeeOverride ?? null, sectionId: v.sectionId ?? null,
      permanentAddress: this.sameAddress() ? v.presentAddress : v.permanentAddress,
      phone: v.phone || v.guardians.find((g) => g.isPrimary)?.phone || null,
    };
    try {
      const s = this.isEdit() ? await this.api.update(+this.id()!, payload) : await this.api.create(payload);
      this.dirty.set(false);
      if (!this.isEdit() && !this.application()) try { localStorage.removeItem(DRAFT_KEY); } catch { /* ignore */ }
      if (!this.isEdit() && this.application()) await this.office.admitted(+this.application()!, s.id).catch(() => {});
      this.toast.success(this.isEdit() ? tr('পরিবর্তন সংরক্ষণ হয়েছে') : tr('{name} এর ভর্তি সম্পন্ন হয়েছে (আইডি {code})', { name: s.name, code: s.studentCode }));
      if (printAfter) this.print.print({ kind: 'admission', data: s });
      this.router.navigate(['/students', s.id]);
    } catch (e) {
      const errs = (e as { error?: { errors?: Record<string, string> } })?.error?.errors;
      if (errs) {
        this.serverErrors.set(Object.fromEntries(Object.entries(errs).map(([k, m]) => [k.replace(/^guardians\[(\d+)\]\./, 'guardians[$1].'), m])));
        const first = Object.keys(errs)[0];
        if (first) this.step.set(this.stepOf(first));
        this.focusFirstError();
      }
      this.toast.error(errorMessage(e));
    } finally { this.saving.set(false); }
  }
}
