import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { DatePipe, fmtDigits, NumPipe } from '../../core/format';
import { WeekPoint } from './hifz.models';
import { TrPipe } from '../../core/tr';

/** Pages of new sabak per week, one series: thin bars, hover tooltip, table for screen readers. */
@Component({
  selector: 'app-week-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, DatePipe, NumPipe],
  template: `
    <div class="relative" (mouseleave)="hover.set(null)">
      <div class="relative h-[130px] flex items-end gap-3 border-b border-line" aria-hidden="true">
        <span class="absolute left-0 right-0 top-0 border-t border-dashed border-line"></span>
        @for (w of data(); track w.weekStart; let i = $index) {
          <div class="flex-1 h-full flex flex-col justify-end items-center gap-1" (mouseenter)="hover.set(i)">
            <small class="text-[11px] text-mut tabular" [class.invisible]="!w.pages">{{ w.pages | num }}</small>
            <i class="block w-[min(28px,70%)] rounded-t-[4px] bg-[var(--chart-1)] transition-opacity" [style.height.%]="(w.pages / max()) * 85"
              [style.opacity]="hover() === null || hover() === i ? 1 : .45"></i>
          </div>
        }
      </div>
      <div class="flex gap-3 mt-1.5 text-[11px] text-mut" aria-hidden="true">
        @for (w of data(); track w.weekStart; let last = $last) { <span class="flex-1 text-center">{{ last ? ('এই সপ্তাহ' | tr) : short(w.weekStart) }}</span> }
      </div>
      @if (hover() !== null) {
        @let w = data()[hover()!];
        <div class="absolute top-0 px-3 py-2 rounded-lg bg-ink text-card text-[13px] shadow-lg pointer-events-none whitespace-nowrap -translate-x-1/2 -translate-y-[70%] z-10"
          [style.left.%]="((hover()! + .5) / data().length) * 100">
          <b class="block">{{ '{d} থেকে' | tr: { d: (w.weekStart | bdate) } }}</b>{{ '{p} পৃষ্ঠা সবক · {d} দিন' | tr: { p: (w.pages | num), d: (w.days | num) } }}<br>{{ 'গড় ভুল {n}' | tr: { n: (w.avgMistakes | num) } }}</div>
      }
      <table class="sr-only"><caption>{{ 'সাপ্তাহিক সবক (পৃষ্ঠা)' | tr }}</caption><tr><th>{{ 'সপ্তাহ শুরু' | tr }}</th><th>{{ 'পৃষ্ঠা' | tr }}</th><th>{{ 'গড় ভুল' | tr }}</th></tr>
        @for (w of data(); track w.weekStart) { <tr><td>{{ w.weekStart | bdate }}</td><td>{{ w.pages | num }}</td><td>{{ w.avgMistakes | num }}</td></tr> }</table>
    </div>
  `,
})
export class WeekChart {
  readonly data = input.required<WeekPoint[]>();
  protected hover = signal<number | null>(null);
  /** "2026-10-17" → "১৭/১০" */
  protected short = (d: string) => fmtDigits(`${+d.slice(8, 10)}/${+d.slice(5, 7)}`);
  protected max = computed(() => Math.max(1, ...this.data().map((w) => w.pages)));
}
