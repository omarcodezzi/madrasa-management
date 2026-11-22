import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { DatePipe, MoneyPipe, MonthPipe, NumPipe } from '../../core/format';
import { TrPipe, tr } from '../../core/tr';

/**
 * Daily attendance %, one series: thin bars, 4px rounded tops anchored to the baseline,
 * recessive gridlines at 50/100, hover tooltip per bar, table fallback for screen readers.
 */
@Component({
  selector: 'app-trend-bars',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, DatePipe, NumPipe],
  template: `
    <div class="relative" (mouseleave)="hover.set(null)">
      <div class="relative h-[150px] mt-4 flex items-end gap-[2px] pl-8" aria-hidden="true">
        <div class="absolute inset-0 left-8 pointer-events-none">
          @for (g of [100, 50]; track g) {
            <span class="absolute left-0 right-0 border-t border-dashed border-line" [style.bottom.%]="g"></span>
            <small class="absolute -left-8 w-7 text-right text-[11px] text-mut -translate-y-1/2" [style.bottom.%]="g" style="transform: translateY(50%)">{{ g | num }}%</small>
          }
          <span class="absolute left-0 right-0 bottom-0 border-t border-line"></span>
        </div>
        @for (p of points(); track p.date; let i = $index) {
          <div class="relative flex-1 h-full flex items-end cursor-default" (mouseenter)="hover.set(i)">
            <i class="block w-full rounded-t-[4px] transition-opacity" [style.height.%]="p.percent"
              [style.background]="p.date === today() ? 'var(--chart-2)' : 'var(--chart-1)'" [style.opacity]="hover() === null || hover() === i ? 1 : .45"></i>
          </div>
        }
      </div>
      <div class="flex justify-between pl-8 mt-1.5 text-[12px] text-mut" aria-hidden="true">
        <span>{{ points()[0]?.date | bdate }}</span><span>{{ 'আজ' | tr }}</span>
      </div>
      @if (hover() !== null) {
        @let p = points()[hover()!];
        <div class="absolute -top-2 px-2.5 py-1.5 rounded-lg bg-ink text-card text-[13px] shadow-lg pointer-events-none whitespace-nowrap -translate-x-1/2 -translate-y-full"
          [style.left]="'calc(2rem + (100% - 2rem) * ' + (hover()! + .5) / points().length + ')'">
          {{ p.date | bdate }} · <b>{{ p.percent | num }}%</b></div>
      }
      <table class="sr-only"><caption>{{ 'দৈনিক উপস্থিতির হার' | tr }}</caption><tr><th>{{ 'তারিখ' | tr }}</th><th>{{ 'হার' | tr }}</th></tr>
        @for (p of points(); track p.date) { <tr><td>{{ p.date | bdate }}</td><td>{{ p.percent | num }}%</td></tr> }</table>
    </div>
  `,
})
export class TrendBars {
  readonly points = input.required<{ date: string; percent: number }[]>();
  readonly today = input<string>('');
  protected hover = signal<number | null>(null);
}

/**
 * Income vs expense by month: grouped bars, two series (chart-1 green, chart-2 gold),
 * legend always shown, 2px gap between paired bars, hover tooltip, sr-only table.
 */
@Component({
  selector: 'app-money-bars',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, MoneyPipe, MonthPipe],
  template: `
    <div class="flex gap-4 text-[13px] text-mut mb-3" aria-hidden="true">
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded-[3px] bg-[var(--chart-1)]"></i> {{ 'আয়' | tr }}</span>
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded-[3px] bg-[var(--chart-2)]"></i> {{ 'ব্যয়' | tr }}</span>
    </div>
    <div class="relative" (mouseleave)="hover.set(null)">
      <div class="relative h-[170px] flex items-end gap-3" aria-hidden="true">
        <span class="absolute left-0 right-0 top-0 border-t border-dashed border-line"></span>
        <span class="absolute left-0 right-0 top-1/2 border-t border-dashed border-line"></span>
        <span class="absolute left-0 right-0 bottom-0 border-t border-line"></span>
        @for (p of data(); track p.month; let i = $index) {
          <div class="relative flex-1 h-full flex items-end justify-center gap-[2px]" (mouseenter)="hover.set(i)" [style.opacity]="hover() === null || hover() === i ? 1 : .5">
            <i class="block w-[min(22px,40%)] rounded-t-[4px] bg-[var(--chart-1)]" [style.height.%]="(p.income / max()) * 100"></i>
            <i class="block w-[min(22px,40%)] rounded-t-[4px] bg-[var(--chart-2)]" [style.height.%]="(p.expense / max()) * 100"></i>
          </div>
        }
      </div>
      <div class="flex gap-3 mt-1.5 text-[12px] text-mut" aria-hidden="true">
        @for (p of data(); track p.month) { <span class="flex-1 text-center">{{ short(p.month) }}</span> }
      </div>
      @if (hover() !== null) {
        @let p = data()[hover()!];
        <div class="absolute top-0 px-3 py-2 rounded-lg bg-ink text-card text-[13px] shadow-lg pointer-events-none whitespace-nowrap -translate-x-1/2 -translate-y-[60%] z-10"
          [style.left.%]="((hover()! + .5) / data().length) * 100">
          <b class="block mb-0.5">{{ p.month | month }}</b>
          {{ 'আয় {m}' | tr: { m: (p.income | money) } }}<br>{{ 'ব্যয় {m}' | tr: { m: (p.expense | money) } }}<br>
          <span class="opacity-80">{{ 'উদ্বৃত্ত {m}' | tr: { m: ((p.income - p.expense) | money) } }}</span></div>
      }
      <table class="sr-only"><caption>{{ 'মাসিক আয় ও ব্যয়' | tr }}</caption><tr><th>{{ 'মাস' | tr }}</th><th>{{ 'আয়' | tr }}</th><th>{{ 'ব্যয়' | tr }}</th></tr>
        @for (p of data(); track p.month) { <tr><td>{{ p.month | month }}</td><td>{{ p.income | money }}</td><td>{{ p.expense | money }}</td></tr> }</table>
    </div>
  `,
})
export class MoneyBars {
  readonly data = input.required<{ month: string; income: number; expense: number }[]>();
  protected hover = signal<number | null>(null);
  protected max = computed(() => Math.max(1, ...this.data().flatMap((d) => [d.income, d.expense])) * 1.05);
  private names = [tr('জানু'), tr('ফেব্রু'), tr('মার্চ'), tr('এপ্রি'), tr('মে'), tr('জুন'), tr('জুলা'), tr('আগ'), tr('সেপ্টে'), tr('অক্টো'), tr('নভে'), tr('ডিসে')];
  protected short = (ym: string) => this.names[+ym.slice(5, 7) - 1];
}
