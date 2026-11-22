import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Button } from './button';
import { Icon } from './icon';
import { TrPipe } from '../../core/tr';

/** Page-level "couldn't load" banner with a retry button. Skeletons after it are hidden (see styles.css). */
@Component({
  selector: 'm-load-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, Button, Icon],
  host: { class: 'mb-4 flex items-center gap-3 px-4 py-3 rounded-xl bg-redbg text-red', role: 'alert' },
  template: `
    <m-icon name="alert" /><span class="flex-1">{{ message() }}</span>
    <button mButton size="sm" variant="outline" type="button" (click)="retry.emit()">{{ 'আবার চেষ্টা করুন' | tr }}</button>
  `,
})
export class LoadError {
  readonly message = input.required<string>();
  readonly retry = output<void>();
}
