import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NumPipe, fmt } from '../../core/format';
import { JuzCell, PAGES_PER_JUZ } from './hifz.models';
import { TrPipe, tr } from '../../core/tr';

/** Progress ring: completed juz out of 30 (conic gradient, as in the prototype). */
@Component({
  selector: 'app-juz-ring',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, NumPipe],
  template: `
    <div class="ring" [style.--p]="(done() / 30) * 100" role="img" [attr.aria-label]="'৩০ পারার মধ্যে {n} পারা সম্পন্ন' | tr: { n: done() }">
      <div><span><b>{{ done() | num }}</b><br><span class="text-[12px] text-mut">{{ 'পারা সম্পন্ন' | tr }}</span></span></div>
    </div>
  `,
})
export class JuzRing { readonly done = input.required<number>(); }

/**
 * 30-juz map. Green = memorised, gold outline = in progress (with a fill showing pages done).
 * When editable, tapping a cell marks it complete or undoes it.
 */
@Component({
  selector: 'app-juz-grid',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, NumPipe],
  template: `
    <div class="juz" role="group" [attr.aria-label]="'৩০ পারা' | tr">
      @for (c of cells(); track c.juzNo) {
        <button type="button" [class.on]="c.status === 'Completed'" [class.cur]="c.status === 'InProgress'" [disabled]="!editable()"
          [attr.aria-pressed]="c.status === 'Completed'" [title]="title(c)" (click)="toggle.emit(c)">
          @if (c.status === 'InProgress' && c.pagesDone) { <i [style.height.%]="(c.pagesDone / pages) * 100"></i> }
          <span>{{ c.juzNo | num }}</span>
        </button>
      }
    </div>
    <p class="m-0 mt-3 flex flex-wrap gap-x-4 gap-y-1 text-[13px] text-mut">
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded bg-g2"></i> {{ 'মুখস্থ সম্পন্ন' | tr }}</span>
      <span class="inline-flex items-center gap-1.5"><i class="size-3 rounded border-2 border-[var(--gold)]"></i> {{ 'এখন চলছে (রং = কত পৃষ্ঠা হয়েছে)' | tr }}</span>
      @if (editable()) { <span>{{ 'ঘরে চাপ দিলে পারা সম্পন্ন হিসেবে চিহ্নিত হবে' | tr }}</span> }
    </p>
  `,
})
export class JuzGrid {
  readonly cells = input.required<JuzCell[]>();
  readonly editable = input(false);
  readonly toggle = output<JuzCell>();
  protected pages = PAGES_PER_JUZ;
  protected title = (c: JuzCell) => tr('পারা {n}', { n: fmt(c.juzNo) }) + ': ' + (c.status === 'Completed' ? tr('সম্পন্ন') : c.status === 'InProgress' ? tr('{a}/{b} পৃষ্ঠা', { a: fmt(c.pagesDone), b: fmt(PAGES_PER_JUZ) }) : tr('শুরু হয়নি'));
  protected count = computed(() => this.cells().filter((c) => c.status === 'Completed').length);
}
