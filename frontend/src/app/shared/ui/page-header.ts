import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Every page starts with this: title + short description + primary action on the right (spec 3). */
@Component({
  selector: 'm-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-wrap gap-x-5 gap-y-3 items-end justify-between mb-5' },
  template: `
    <div>
      <h1>{{ title() }}</h1>
      @if (description()) {<p class="m-0 mt-1.5 text-mut">{{ description() }}</p>}
    </div>
    <div class="flex flex-wrap gap-2"><ng-content /></div>
  `,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly description = input<string>();
}
