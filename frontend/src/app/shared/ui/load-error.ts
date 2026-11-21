import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Button } from './button';
import { Icon } from './icon';

/** Page-level "couldn't load" banner with a retry button. Skeletons after it are hidden (see styles.css). */
@Component({
  selector: 'm-load-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Icon],
  host: { class: 'mb-4 flex items-center gap-3 px-4 py-3 rounded-xl bg-redbg text-red', role: 'alert' },
  template: `
    <m-icon name="alert" /><span class="flex-1">{{ message() }}</span>
    <button mButton size="sm" variant="outline" type="button" (click)="retry.emit()">আবার চেষ্টা করুন</button>
  `,
})
export class LoadError {
  readonly message = input.required<string>();
  readonly retry = output<void>();
}
