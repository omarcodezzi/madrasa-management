import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { errorMessage, fileUrl } from '../../core/api';
import { AuthService } from '../../core/auth/auth.service';
import { addMonths, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, todayIso } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { Avatar, Badge, Card, EmptyState, PageHeader, Skeleton } from '../../shared/ui';
import { JuzGrid, JuzRing } from '../hifz/juz-grid';
import { OfficeApi } from '../office/office.api';
import { PortalHome } from '../office/office.models';
import { AttendanceCalendar } from '../students/attendance-calendar';
import { TrPipe } from '../../core/tr';

type Detail = Awaited<ReturnType<OfficeApi['portalStudent']>>;

/** Guardian / student view: read-only, only their own children, only published results. */
@Component({
  selector: 'app-portal-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, NumPipe, Avatar, Badge, Card, EmptyState, PageHeader, Skeleton, AttendanceCalendar, JuzGrid, JuzRing],
  templateUrl: './portal-page.html',
})
export class PortalPage {
  private api = inject(OfficeApi);
  private toast = inject(ToastService);
  protected auth = inject(AuthService);
  protected home = signal<PortalHome | null>(null);
  protected error = signal<string | null>(null);
  protected childId = signal<number | null>(null);
  protected month = signal(todayIso().slice(0, 7));
  protected detail = signal<Detail | null>(null);
  protected today = todayIso();
  protected photo = (p: string | null | undefined) => fileUrl(p);
  protected latest = computed(() => this.detail()?.results[0] ?? null);

  constructor() {
    this.api.portal().then((h) => { this.home.set(h); if (h.children.length) this.childId.set(h.children[0].studentId); })
      .catch((e) => this.error.set(errorMessage(e)));
    effect(() => { const id = this.childId(), m = this.month(); if (id) untracked(() => this.load(id, m)); });
  }
  private async load(id: number, m: string) {
    try { const d = await this.api.portalStudent(id, m); this.detail.set(d); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected pick(id: number) { if (id !== this.childId()) { this.detail.set(null); this.childId.set(id); } }
  protected move(n: number) { this.month.set(addMonths(this.month(), n)); }
}
