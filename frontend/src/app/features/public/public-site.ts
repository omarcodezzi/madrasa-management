import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { errorMessage, fileUrl } from '../../core/api';
import { DatePipe, DigitsPipe, MoneyPipe, NumPipe, toEnDigits, todayIso } from '../../core/format';
import { Badge, Button, FormField, Icon, Skeleton, resizePhoto } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { blankApply } from '../admissions/admissions-page';
import { OfficeApi } from '../office/office.api';
import { ApplicationStatus, ApplyInput, EVENT_LABEL, PublicInfo, STAGE_LABEL } from '../office/office.models';

/** The madrasa's public page: about, departments and fees, notices, events, online admission and status check. No sign-in. */
@Component({
  selector: 'app-public-site',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DatePipe, DigitsPipe, MoneyPipe, NumPipe, Badge, Button, FormField, Icon, Skeleton],
  templateUrl: './public-site.html',
})
export class PublicSite {
  private api = inject(OfficeApi);
  protected info = signal<PublicInfo | null>(null);
  protected error = signal<string | null>(null);
  protected logo = computed(() => fileUrl(this.info()?.logoPath) ?? '/logo.jpg');
  protected departments = computed(() => {
    const m = new Map<string, PublicInfo['classes']>();
    for (const c of this.info()?.classes ?? []) m.set(c.department, [...(m.get(c.department) ?? []), c]);
    return [...m.entries()].map(([name, classes]) => ({ name, classes }));
  });
  protected eLabel = (k: string) => (EVENT_LABEL as Record<string, string>)[k] ?? k;
  protected sLabel = (s: string) => (STAGE_LABEL as Record<string, string>)[s] ?? s;
  protected today = todayIso();

  protected form = signal<ApplyInput>({ ...blankApply(), website: '' });
  protected preview = signal<string | null>(null);
  protected errs = signal<Record<string, string>>({});
  protected sending = signal(false);
  protected done = signal<{ applicationNo: string; message: string } | null>(null);
  protected formError = signal<string | null>(null);

  protected stNo = signal('');
  protected stPhone = signal('');
  protected status = signal<ApplicationStatus | null>(null);
  protected stError = signal<string | null>(null);
  protected checking = signal(false);

  constructor() {
    this.api.publicInfo().then((i) => { this.info.set(i); document.title = i.nameBn; if (i.classes.length) this.patch({ classId: i.classes[0].id }); })
      .catch((e) => this.error.set(errorMessage(e)));
  }
  protected patch(p: Partial<ApplyInput>) { this.form.update((f) => ({ ...f, ...p })); }
  protected async pickPhoto(ev: Event) {
    const f = (ev.target as HTMLInputElement).files?.[0]; if (!f) return;
    try { const d = await resizePhoto(f); this.preview.set(d); this.patch({ photo: d }); } catch (e) { this.errs.set({ photo: (e as Error).message }); }
  }
  protected async apply() {
    const f = this.form(); const e: Record<string, string> = {};
    if (!f.applicantName.trim()) e['applicantName'] = 'শিক্ষার্থীর নাম লিখুন';
    if (!/^01\d{9}$/.test(toEnDigits(f.guardianPhone).replace(/\D/g, ''))) e['guardianPhone'] = 'সঠিক মোবাইল নম্বর দিন (০১ দিয়ে শুরু, ১১ সংখ্যা)';
    if (Object.keys(e).length) { this.errs.set(e); return; }
    this.sending.set(true); this.errs.set({}); this.formError.set(null);
    try {
      this.done.set(await this.api.apply({ ...f, guardianPhone: toEnDigits(f.guardianPhone), dateOfBirth: f.dateOfBirth || null }));
      this.stNo.set(this.done()!.applicationNo); this.stPhone.set(toEnDigits(f.guardianPhone));
    } catch (err) { const fe = fieldErrors(err); this.errs.set(fe); if (!Object.keys(fe).length) this.formError.set(errorMessage(err)); }
    finally { this.sending.set(false); }
  }
  protected again() { this.done.set(null); this.preview.set(null); this.form.set({ ...blankApply(), classId: this.info()?.classes[0]?.id ?? 0, website: '' }); }
  protected async check() {
    this.checking.set(true); this.stError.set(null); this.status.set(null);
    try { this.status.set(await this.api.status(this.stNo().trim(), toEnDigits(this.stPhone()))); }
    catch (e) { this.stError.set(errorMessage(e)); }
    finally { this.checking.set(false); }
  }
}
