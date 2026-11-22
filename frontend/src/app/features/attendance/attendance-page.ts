import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AttendanceStatus, errorMessage, fileUrl } from '../../core/api';
import { DatePipe, DigitsPipe, NumPipe, TimePipe, todayIso, fmt } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { ToastService } from '../../core/toast.service';
import { Avatar, Badge, Button, Card, Chips, EmptyState, Icon, IconButton, PageHeader, Segmented, Skeleton } from '../../shared/ui';
import { ATT_LABEL, ATT_TONE, AttendanceDay, AttendanceRow, ScanResult } from './attendance.models';
import { AttendanceApi, beep } from './attendance.service';
import { QrScanner } from './qr-scanner';
import { TrPipe, tr, trApi } from '../../core/tr';

type Mode = 'roll' | 'scan';
interface LogItem extends ScanResult { at: number; }

@Component({
  selector: 'app-attendance-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, FormsModule, RouterLink, DatePipe, DigitsPipe, NumPipe, TimePipe, Avatar, Badge, Button, Card, Chips, EmptyState, Icon, IconButton, PageHeader, Segmented, Skeleton, QrScanner],
  templateUrl: './attendance-page.html',
})
export class AttendancePage {
  private api = inject(AttendanceApi);
  private toast = inject(ToastService);
  protected lookups = inject(LookupsService);

  protected today = todayIso();
  protected date = signal(this.today);
  protected classId = signal(0);
  protected mode = signal<Mode>('roll');
  protected modes = [{ value: 'roll' as Mode, label: tr('রোল-কল') }, { value: 'scan' as Mode, label: tr('QR স্ক্যান') }];
  protected day = signal<AttendanceDay | null>(null);
  protected error = signal<string | null>(null);
  protected saving = signal<Set<number>>(new Set());
  protected log = signal<LogItem[]>([]);
  protected code = '';
  protected scanning = signal(false);
  protected label = ATT_LABEL;
  protected tone = ATT_TONE;
  protected statuses: AttendanceStatus[] = ['Present', 'Absent', 'Late', 'Leave'];
  protected short: Record<AttendanceStatus, string> = { Present: tr('উ'), Absent: tr('অ'), Late: tr('দে'), Leave: tr('ছু') };
  protected photo = fileUrl;

  protected classOptions = computed(() => [{ value: 0, label: tr('সব শ্রেণি') }, ...this.lookups.classes().map((c) => ({ value: c.id, label: c.nameBn }))]);
  protected rows = computed(() => { const d = this.day(); const c = this.classId(); return d ? d.rows.filter((r) => !c || r.classId === c) : []; });
  protected stats = computed(() => {
    const r = this.rows(); const n = (s: AttendanceStatus) => r.filter((x) => x.status === s).length;
    const present = n('Present'), late = n('Late'), absent = n('Absent'), leave = n('Leave');
    return { total: r.length, present, late, absent, leave, unmarked: r.length - present - late - absent - leave,
      percent: r.length ? Math.round(((present + late) * 100) / r.length) : 0 };
  });
  protected absentees = computed(() => this.rows().filter((r) => r.status === 'Absent'));
  protected groups = computed(() => {
    const m = new Map<string, AttendanceRow[]>();
    for (const r of this.rows()) m.set(r.className, [...(m.get(r.className) ?? []), r]);
    return [...m.entries()].map(([name, rows]) => ({ name, rows }));
  });
  protected isFuture = computed(() => this.date() > this.today);

  constructor() {
    effect(() => { const d = this.date(); untracked(() => this.load(d)); });
  }

  async load(d = this.date()) {
    this.error.set(null);
    try { this.day.set(await this.api.day(d)); } catch (e) { this.error.set(errorMessage(e)); }
  }

  /** One tap marks at once; tapping the active status again clears it. */
  protected async set(r: AttendanceRow, status: AttendanceStatus) {
    const next = r.status === status ? null : status;
    this.patch([r.studentId], next);
    this.saving.update((s) => new Set(s).add(r.studentId));
    try { await this.api.mark(this.date(), [{ studentId: r.studentId, status: next }]); }
    catch (e) { this.patch([r.studentId], r.status ?? null); this.toast.error(errorMessage(e)); }
    finally { this.saving.update((s) => { const n = new Set(s); n.delete(r.studentId); return n; }); }
  }

  protected async allPresent() {
    const ids = this.rows().filter((r) => !r.status).map((r) => r.studentId);
    if (!ids.length) { this.toast.success(tr('সবার হাজিরা আগেই দেওয়া হয়েছে')); return; }
    this.patch(ids, 'Present');
    try {
      await this.api.mark(this.date(), ids.map((studentId) => ({ studentId, status: 'Present' as const })));
      this.toast.success(tr('{n} জনকে উপস্থিত দেওয়া হয়েছে', { n: fmt(ids.length) }));
    } catch (e) { this.patch(ids, null); this.toast.error(errorMessage(e)); }
  }

  private patch(ids: number[], status: AttendanceStatus | null) {
    const set = new Set(ids);
    this.day.update((d) => d && { ...d, rows: d.rows.map((r) => (set.has(r.studentId) ? { ...r, status } : r)) });
  }

  protected async scan(code: string) {
    code = code.trim();
    if (!code || this.scanning()) return;
    this.scanning.set(true);
    try {
      const raw = await this.api.scan(code, this.date());
      const res = { ...raw, message: trApi(raw.message) };
      beep(res.ok);
      this.log.update((l) => [{ ...res, at: Date.now() }, ...l].slice(0, 30));
      if (res.ok && res.studentId) {
        this.day.update((d) => d && { ...d, rows: d.rows.map((r) => (r.studentId === res.studentId ? { ...r, status: r.status ?? 'Present', checkIn: r.checkIn ?? res.checkIn } : r)) });
      }
      this.code = '';
    } catch (e) { beep(false); this.toast.error(errorMessage(e)); }
    finally { this.scanning.set(false); }
  }
}
