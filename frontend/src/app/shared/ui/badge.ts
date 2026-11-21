import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type BadgeTone = 'success' | 'danger' | 'warning' | 'info' | 'gold' | 'neutral';
const TONE: Record<BadgeTone, string> = {
  success: 'bg-okbg text-g2',
  danger: 'bg-redbg text-red',
  warning: 'bg-[var(--warnbg)] text-[var(--warn)]',
  info: 'bg-[var(--infobg)] text-[var(--info)]',
  gold: 'bg-goldbg text-goldink',
  neutral: 'bg-soft text-mut',
};

@Component({
  selector: 'm-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'cls()' },
  template: `@if (dot()) {<span class="size-1.5 rounded-full bg-current" aria-hidden="true"></span>}<ng-content />`,
})
export class Badge {
  readonly tone = input<BadgeTone>('neutral');
  readonly dot = input(false);
  protected cls = computed(
    () => 'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[13px] font-semibold whitespace-nowrap ' + TONE[this.tone()],
  );
}
