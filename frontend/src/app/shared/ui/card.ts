import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Panel from the prototype: white card, 1px border, 16px radius. */
@Component({
  selector: 'm-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block bg-card border border-line rounded-[var(--radius-card)] shadow-[var(--shadow)]', '[class.p-5]': 'padded()' },
  template: `
    @if (title()) {
      <div class="flex items-center justify-between gap-3 flex-wrap mb-3.5" [class.px-5]="!padded()" [class.pt-4]="!padded()">
        <div>
          <h2 class="text-[19px]">{{ title() }}</h2>
          @if (subtitle()) {<p class="text-sm text-mut m-0 mt-1">{{ subtitle() }}</p>}
        </div>
        <ng-content select="[cardActions]" />
      </div>
    }
    <ng-content />
  `,
})
export class Card {
  readonly title = input<string>();
  readonly subtitle = input<string>();
  readonly padded = input(true);
}
