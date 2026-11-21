import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon, IconName } from './icon';

/** Friendly empty state with the faint star pattern (allowed here per spec 2.3). */
@Component({
  selector: 'm-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  host: { class: 'block text-center px-4 py-10 rounded-[var(--radius-card)] pattern' },
  template: `
    <div class="mx-auto mb-3 size-14 rounded-full grid place-items-center bg-soft text-g2"><m-icon [name]="icon()" [size]="26" /></div>
    <b class="block text-ink text-[17px] mb-1">{{ title() }}</b>
    @if (description()) {<p class="m-0 text-mut">{{ description() }}</p>}
    <div class="mt-4 flex justify-center gap-2"><ng-content /></div>
  `,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly description = input<string>();
  readonly icon = input<IconName>('users');
}
