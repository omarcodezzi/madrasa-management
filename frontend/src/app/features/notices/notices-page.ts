import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { DatePipe, DigitsPipe, MONTHS_BN, NumPipe, todayIso } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { Badge, BadgeTone, Button, Card, ConfirmService, EmptyState, FormField, Icon, LoadError, Modal, PageHeader, Segmented, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { Audience, AUDIENCE_LABEL, EVENT_LABEL, EventKind, MessageRow, Notice, NoticeInput, SchoolEvent } from '../office/office.models';

type Tab = 'board' | 'events' | 'sms';
const EVENT_TONE: Record<EventKind, BadgeTone> = { General: 'success', Exam: 'warning', Holiday: 'info', Meeting: 'gold' };

@Component({
  selector: 'app-notices-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, DigitsPipe, NumPipe, Badge, Button, Card, EmptyState, FormField, Icon, LoadError, Modal, PageHeader, Segmented, Skeleton],
  templateUrl: './notices-page.html',
})
export class NoticesPage {
  private api = inject(OfficeApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private auth = inject(AuthService);
  protected canManage = computed(() => this.auth.can(P.notices));

  protected tab = signal<Tab>('board');
  protected tabs = [{ value: 'board' as Tab, label: 'নোটিশ বোর্ড' }, { value: 'events' as Tab, label: 'ইভেন্ট ও ছুটি' }, { value: 'sms' as Tab, label: 'SMS তালিকা' }];
  protected notices = signal<Notice[] | null>(null);
  protected events = signal<SchoolEvent[] | null>(null);
  protected messages = signal<MessageRow[] | null>(null);
  protected audiences = Object.entries(AUDIENCE_LABEL).map(([value, label]) => ({ value: value as Audience, label }));
  protected kinds = Object.entries(EVENT_LABEL).map(([value, label]) => ({ value: value as EventKind, label }));
  protected aLabel = (a: string) => (AUDIENCE_LABEL as Record<string, string>)[a] ?? a;
  protected eLabel = (k: string) => (EVENT_LABEL as Record<string, string>)[k] ?? k;
  protected eTone = (k: string) => (EVENT_TONE as Record<string, BadgeTone>)[k] ?? 'neutral';
  protected today = todayIso();
  protected byMonth = computed(() => {
    const groups = new Map<string, SchoolEvent[]>();
    for (const e of this.events() ?? []) { const k = e.startDate.slice(0, 7); groups.set(k, [...(groups.get(k) ?? []), e]); }
    return [...groups.entries()].map(([m, list]) => ({ m, label: `${MONTHS_BN[+m.slice(5) - 1]} ${m.slice(0, 4)}`, list }));
  });

  // notice form
  protected nOpen = signal(false);
  protected nId = signal<number | null>(null);
  protected nForm = signal<NoticeInput>({ title: '', body: '', date: todayIso(), audience: 'All', isPublic: true, sendSms: false });
  protected nErrs = signal<Record<string, string>>({});
  protected saving = signal(false);

  // event form
  protected eOpen = signal(false);
  protected eId = signal<number | null>(null);
  protected eForm = signal({ title: '', startDate: todayIso(), endDate: '', kind: 'General' as EventKind });
  protected eErrs = signal<Record<string, string>>({});

  protected loadError = signal<string | null>(null);
  private failed(e: unknown) { this.loadError.set(errorMessage(e, 'তথ্য লোড করা যায়নি।')); }
  protected retry() { this.loadError.set(null); this.load(this.tab()); }

  constructor() {
    effect(() => { const t = this.tab(); untracked(() => this.load(t)); });
  }
  private async load(t: Tab) {
    try {
      if (t === 'board') this.notices.set(await this.api.notices());
      if (t === 'events') this.events.set(await this.api.events(this.today.slice(0, 7) + '-01'));
      if (t === 'sms') this.messages.set(await this.api.messages());
    } catch (e) { this.failed(e); }
  }

  protected newNotice() { this.nId.set(null); this.nErrs.set({}); this.nForm.set({ title: '', body: '', date: todayIso(), audience: 'All', isPublic: true, sendSms: false }); this.nOpen.set(true); }
  protected editNotice(n: Notice) { this.nId.set(n.id); this.nErrs.set({}); this.nForm.set({ title: n.title, body: n.body, date: n.date, audience: n.audience, isPublic: n.isPublic, sendSms: false }); this.nOpen.set(true); }
  protected patchN(p: Partial<NoticeInput>) { this.nForm.update((f) => ({ ...f, ...p })); }
  protected async saveNotice() {
    this.saving.set(true); this.nErrs.set({});
    try {
      const r = await this.api.saveNotice(this.nId(), this.nForm());
      this.toast.success(r.smsQueued ? `নোটিশ প্রকাশ হয়েছে। ${r.smsQueued.toLocaleString('bn-BD')}টি SMS পাঠানোর তালিকায় রাখা হয়েছে।` : 'নোটিশ সংরক্ষণ হয়েছে');
      this.nOpen.set(false); this.load('board');
    } catch (e) { const fe = fieldErrors(e); this.nErrs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.saving.set(false); }
  }
  protected async deleteNotice(n: Notice) {
    if (!(await this.confirm.ask({ message: 'নোটিশটি বোর্ড ও ওয়েবসাইট থেকে মুছে যাবে।', itemName: n.title, confirmLabel: 'মুছে ফেলুন' }))) return;
    try { await this.api.deleteNotice(n.id); this.toast.success('নোটিশ মুছে ফেলা হয়েছে'); this.load('board'); } catch (e) { this.toast.error(errorMessage(e)); }
  }

  protected newEvent() { this.eId.set(null); this.eErrs.set({}); this.eForm.set({ title: '', startDate: todayIso(), endDate: '', kind: 'General' }); this.eOpen.set(true); }
  protected editEvent(e: SchoolEvent) { this.eId.set(e.id); this.eErrs.set({}); this.eForm.set({ title: e.title, startDate: e.startDate, endDate: e.endDate ?? '', kind: e.kind }); this.eOpen.set(true); }
  protected patchE(p: Partial<{ title: string; startDate: string; endDate: string; kind: EventKind }>) { this.eForm.update((f) => ({ ...f, ...p })); }
  protected async saveEvent() {
    const f = this.eForm(); this.saving.set(true); this.eErrs.set({});
    try {
      await this.api.saveEvent(this.eId(), { title: f.title, startDate: f.startDate, endDate: f.endDate || null, kind: f.kind });
      this.toast.success('ইভেন্ট সংরক্ষণ হয়েছে'); this.eOpen.set(false); this.load('events');
    } catch (e) { const fe = fieldErrors(e); this.eErrs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.saving.set(false); }
  }
  protected async deleteEvent() {
    const id = this.eId(); if (!id) return;
    if (!(await this.confirm.ask({ message: 'ইভেন্টটি ক্যালেন্ডার থেকে মুছে যাবে।', itemName: this.eForm().title, confirmLabel: 'মুছে ফেলুন' }))) return;
    try { await this.api.deleteEvent(id); this.toast.success('ইভেন্ট মুছে ফেলা হয়েছে'); this.eOpen.set(false); this.load('events'); } catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected day = (iso: string) => iso.slice(8, 10);
}
