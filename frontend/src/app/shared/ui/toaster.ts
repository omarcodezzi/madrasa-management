import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/toast.service';
import { Icon } from './icon';
import { TrPipe } from '../../core/tr';

@Component({
  selector: 'm-toaster',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, Icon],
  host: { class: 'fixed z-50 bottom-5 left-1/2 -translate-x-1/2 grid gap-2 w-[min(420px,calc(100vw-24px))] no-print', 'aria-live': 'polite' },
  template: `
    @for (t of svc.toasts(); track t.id) {
      <div role="status" class="flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg font-medium text-white animate-[toast-in_.18s_ease-out]"
        [class]="t.kind === 'err' ? 'bg-[#b42318]' : t.kind === 'info' ? 'bg-[#12281b]' : 'bg-g'">
        <m-icon [name]="t.kind === 'err' ? 'alert' : t.kind === 'info' ? 'info' : 'check'" />
        <span class="flex-1">{{ t.text }}</span>
        <button type="button" class="opacity-80 hover:opacity-100 cursor-pointer" (click)="svc.dismiss(t.id)" [attr.aria-label]="'বন্ধ করুন' | tr"><m-icon name="x" [size]="18" /></button>
      </div>
    }
  `,
  styles: `@keyframes toast-in { from { opacity: 0; transform: translateY(8px); } }`,
})
export class Toaster {
  protected svc = inject(ToastService);
}
