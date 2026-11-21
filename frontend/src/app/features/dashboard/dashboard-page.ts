import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { API, errorMessage } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { DatePipe, MoneyPipe, NumPipe, hijriDate } from '../../core/format';
import { Button, Card, EmptyState, Icon, IconName, Skeleton } from '../../shared/ui';
import { MoneyBars, TrendBars } from './charts';
import { Dashboard } from './dashboard.models';

@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, MoneyPipe, NumPipe, Button, Card, EmptyState, Icon, Skeleton, MoneyBars, TrendBars],
  templateUrl: './dashboard-page.html',
})
export class DashboardPage {
  private http = inject(HttpClient);
  protected auth = inject(AuthService);
  protected d = signal<Dashboard | null>(null);
  protected error = signal<string | null>(null);
  protected hijri = computed(() => (this.d() ? hijriDate(this.d()!.today) : ''));
  protected greeting = (() => { const h = new Date().getHours(); return h < 12 ? 'শুভ সকাল' : h < 17 ? 'শুভ অপরাহ্ন' : 'শুভ সন্ধ্যা'; })();
  protected feePercent = computed(() => { const d = this.d(); return d && d.expectedThisMonth ? Math.min(100, Math.round((d.collectedThisMonth * 100) / d.expectedThisMonth)) : 0; });
  protected attDelta = computed(() => { const d = this.d(); return d ? Math.round(d.todayPercent - d.yesterdayPercent) : 0; });
  protected deptMax = computed(() => Math.max(1, ...(this.d()?.byDepartment.map((x) => x.count) ?? [])));
  protected hasTrend = computed(() => (this.d()?.attendanceTrend ?? []).some((p) => p.percent > 0));
  protected hasMoney = computed(() => (this.d()?.incomeExpense ?? []).some((m) => m.income > 0 || m.expense > 0));
  protected actions = computed(() => ([
    { icon: 'scan', label: 'হাজিরা নিন', link: '/attendance', perm: P.attendance },
    { icon: 'wallet', label: 'বেতন নিন', link: '/fees', perm: P.fees },
    { icon: 'plus', label: 'নতুন ভর্তি', link: '/admission/new', perm: P.studentsEdit },
    { icon: 'card', label: 'আইডি কার্ড', link: '/idcards', perm: P.studentsView },
  ] as { icon: IconName; label: string; link: string; perm: string }[]).filter((a) => this.auth.can(a.perm)));

  constructor() { this.load(); }
  async load() {
    this.error.set(null);
    try { this.d.set(await firstValueFrom(this.http.get<Dashboard>(`${API}/dashboard`))); } catch (e) { this.error.set(errorMessage(e)); }
  }
}
