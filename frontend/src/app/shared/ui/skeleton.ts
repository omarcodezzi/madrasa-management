import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'm-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block rounded-lg bg-soft animate-pulse',
    '[style.height]': 'height()',
    '[style.width]': 'width()',
    'aria-hidden': 'true',
  },
  template: ``,
})
export class Skeleton {
  readonly height = input('16px');
  readonly width = input('100%');
}
