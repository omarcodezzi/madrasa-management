import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MonthPipe, NumPipe } from '../../core/format';
import { Icon } from '../../shared/ui';
import { DayMark, StudentMonth } from './student.models';

const DOW = ['শনি', 'রবি', 'সোম', 'মঙ্গল', 'বুধ', 'বৃহঃ', 'শুক্র'];
const CELL: Record<string, string> = {
  Present: 'bg-g2 text-white', Late: 'bg-[var(--warn)] text-white', Absent: 'bg-red text-white', Leave: 'bg-[var(--info)] text-white',
};
const LABEL: Record<string, string> = { Present: 'উপস্থিত', Late: 'দেরি', Absent: 'অনুপস্থিত', Leave: 'ছুটি' };

/** Month heatmap: one cell per day; status is also written in the cell's label so colour is never the only cue. */
@Component({
  selector: 'app-attendance-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MonthPipe, NumPipe, Icon],
  template: `
    <div class="flex items-center justify-between gap-3 mb-3">
      <button type="button" class="size-10 grid place-items-center rounded-lg hover:bg-soft cursor-pointer" (click)="move.emit(-1)" aria-label="আগের মাস"><m-icon name="chevronLeft" /></button>
      <b class="font-serif text-lg">{{ data().month | month }}</b>
      <button type="button" class="size-10 grid place-items-center rounded-lg hover:bg-soft cursor-pointer disabled:opacity-30" [disabled]="isCurrent()" (click)="move.emit(1)" aria-label="পরের মাস"><m-icon name="chevronRight" /></button>
    </div>
    <div class="grid grid-cols-7 gap-1.5 text-center" role="grid" [attr.aria-label]="'হাজিরা'">
      @for (d of dow; track d) { <div class="text-xs text-mut font-semibold pb-1" role="columnheader">{{ d }}</div> }
      @for (b of blanks(); track $index) { <div></div> }
      @for (d of data().days; track d.date) {
        <div role="gridcell" class="aspect-square rounded-lg grid place-items-center text-sm font-semibold tabular"
          [class]="cls(d)" [title]="title(d)" [attr.aria-label]="title(d)">{{ +d.date.slice(8) | num }}</div>
      }
    </div>
    <div class="flex flex-wrap gap-x-4 gap-y-1 mt-3 text-sm text-mut">
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded bg-g2"></i>উপস্থিত {{ data().present | num }}</span>
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded bg-[var(--warn)]"></i>দেরি {{ data().late | num }}</span>
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded bg-red"></i>অনুপস্থিত {{ data().absent | num }}</span>
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded bg-[var(--info)]"></i>ছুটি {{ data().leave | num }}</span>
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded border border-line bg-soft"></i>বন্ধ</span>
    </div>
  `,
})
export class AttendanceCalendar {
  readonly data = input.required<StudentMonth>();
  readonly today = input.required<string>();
  readonly move = output<number>();
  protected dow = DOW;
  /** Week starts on Saturday, as in Bangladeshi madrasa calendars */
  protected blanks = computed(() => {
    const first = new Date(this.data().days[0].date + 'T00:00:00Z').getUTCDay(); // 0 Sun..6 Sat
    return Array.from({ length: (first + 1) % 7 });
  });
  protected isCurrent = computed(() => this.data().month >= this.today().slice(0, 7));
  protected cls(d: DayMark) {
    if (d.status) return CELL[d.status];
    if (!d.isWorkDay) return 'bg-soft text-mut/60';
    if (d.date > this.today()) return 'border border-line text-mut';
    if (d.date === this.today()) return 'border-2 border-gold text-ink';
    return 'bg-redbg text-red';   // a passed school day without a mark counts as absent
  }
  protected title(d: DayMark) {
    const label = d.status ? LABEL[d.status] : !d.isWorkDay ? 'বন্ধ' : d.date > this.today() ? '' : d.date === this.today() ? 'আজ, এখনো হাজিরা হয়নি' : 'অনুপস্থিত (হাজিরা নেই)';
    return `${d.date}${label ? ': ' + label : ''}${d.checkIn ? ' ' + d.checkIn.slice(0, 5) : ''}`;
  }
}
