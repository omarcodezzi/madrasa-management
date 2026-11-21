import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { errorMessage, fileUrl, Gender } from '../../core/api';
import { DatePipe, DigitsPipe, NumPipe, toEnDigits } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { ToastService } from '../../core/toast.service';
import { Avatar, Badge, BadgeTone, Button, Card, Chips, EmptyState, FormField, Icon, Modal, PageHeader, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { Application, ApplyInput, Stage, STAGE_LABEL } from '../office/office.models';

const TONE: Record<Stage, BadgeTone> = { Applied: 'info', Test: 'warning', Approved: 'gold', Admitted: 'success', Rejected: 'danger' };
export const blankApply = (): ApplyInput => ({ classId: 0, applicantName: '', gender: 'Male', dateOfBirth: null, fatherName: null, guardianPhone: '', address: null, previousInstitution: null, photo: null });

/** Admission pipeline: Applied → Test → Approved → Admitted (the student form), or Rejected. */
@Component({
  selector: 'app-admissions-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DatePipe, DigitsPipe, NumPipe, Avatar, Badge, Button, Card, Chips, EmptyState, FormField, Icon, Modal, PageHeader, Skeleton],
  templateUrl: './admissions-page.html',
})
export class AdmissionsPage {
  private api = inject(OfficeApi);
  private toast = inject(ToastService);
  private router = inject(Router);
  protected lookups = inject(LookupsService);

  protected stage = signal<'' | Stage>('Applied');
  protected counts = signal<Record<string, number>>({});
  protected options = computed(() => [
    ...(['Applied', 'Test', 'Approved', 'Admitted', 'Rejected'] as Stage[]).map((s) => ({ value: s as '' | Stage, label: `${STAGE_LABEL[s]} (${(this.counts()[s] ?? 0).toLocaleString('bn-BD')})` })),
    { value: '' as '' | Stage, label: 'সব' },
  ]);
  protected rows = signal<Application[] | null>(null);
  protected q = signal('');
  protected filtered = computed(() => {
    const t = toEnDigits(this.q().trim()).toLowerCase();
    return (this.rows() ?? []).filter((a) => !t || a.applicantName.toLowerCase().includes(t) || a.applicationNo.toLowerCase().includes(t) || a.guardianPhone.includes(t));
  });
  protected sLabel = (s: string) => (STAGE_LABEL as Record<string, string>)[s] ?? s;
  protected sTone = (s: string) => (TONE as Record<string, BadgeTone>)[s] ?? 'neutral';
  protected photo = (p: string | null) => fileUrl(p);
  protected gLabel = (g: Gender) => (g === 'Male' ? 'ছাত্র' : 'ছাত্রী');

  // review modal
  protected open = signal(false);
  protected app = signal<Application | null>(null);
  protected marks = signal('');
  protected remarks = signal('');
  protected busy = signal(false);

  // office application
  protected newOpen = signal(false);
  protected form = signal<ApplyInput>(blankApply());
  protected errs = signal<Record<string, string>>({});

  constructor() {
    this.loadCounts();
    effect(() => { const s = this.stage(); untracked(() => this.load(s)); });
  }
  private loadCounts() { this.api.pipeline().then((p) => this.counts.set(Object.fromEntries(p.map((x) => [x.stage, x.count])))).catch(() => {}); }
  private async load(s: '' | Stage) { this.rows.set(null); try { this.rows.set(await this.api.applications(s)); } catch (e) { this.toast.error(errorMessage(e)); } }
  private refresh() { this.loadCounts(); this.load(this.stage()); }

  protected review(a: Application) { this.app.set(a); this.marks.set(a.testMarks === null ? '' : String(a.testMarks)); this.remarks.set(a.remarks ?? ''); this.open.set(true); }
  protected async move(stage: Stage) {
    const a = this.app(); if (!a) return;
    this.busy.set(true);
    try {
      const m = this.marks().trim();
      await this.api.setStage(a.id, { stage, testMarks: m ? +toEnDigits(m) : null, remarks: this.remarks().trim() || null });
      this.toast.success(`${a.applicantName}: ${STAGE_LABEL[stage]}`); this.open.set(false); this.refresh();
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.busy.set(false); }
  }
  protected admit(a: Application) { this.open.set(false); this.router.navigate(['/admission/new'], { queryParams: { application: a.id } }); }

  protected newApplication() { this.form.set({ ...blankApply(), classId: this.lookups.classes()[0]?.id ?? 0 }); this.errs.set({}); this.newOpen.set(true); }
  protected patch(p: Partial<ApplyInput>) { this.form.update((f) => ({ ...f, ...p })); }
  protected async saveApplication() {
    this.busy.set(true); this.errs.set({});
    try {
      const f = this.form();
      const a = await this.api.createApplication({ ...f, guardianPhone: toEnDigits(f.guardianPhone), dateOfBirth: f.dateOfBirth || null });
      this.toast.success(`আবেদন নেওয়া হয়েছে (${a.applicationNo})`); this.newOpen.set(false); this.stage.set('Applied'); this.refresh();
    } catch (e) { const fe = fieldErrors(e); this.errs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.busy.set(false); }
  }
}
