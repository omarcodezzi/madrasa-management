import { ChangeDetectionStrategy, Component, Injectable, inject, signal } from '@angular/core';
import { Button } from './button';
import { Modal } from './modal';
import { TrPipe, tr } from '../../core/tr';

interface ConfirmReq { title: string; message: string; itemName?: string; confirmLabel: string; resolve: (ok: boolean) => void; }

/** `if (await confirm.ask({ message: 'মুছে ফেলবেন?', itemName: s.name })) ...` — destructive actions always show the item (spec 7). */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly current = signal<ConfirmReq | null>(null);
  ask(o: { message: string; itemName?: string; title?: string; confirmLabel?: string }): Promise<boolean> {
    return new Promise((resolve) =>
      this.current.set({ title: o.title ?? tr('নিশ্চিত করুন'), confirmLabel: o.confirmLabel ?? tr('মুছে ফেলুন'), message: o.message, itemName: o.itemName, resolve }),
    );
  }
  answer(ok: boolean) {
    this.current()?.resolve(ok);
    this.current.set(null);
  }
}

@Component({
  selector: 'm-confirm-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, Modal, Button],
  template: `
    @if (svc.current(); as c) {
      <m-modal [open]="true" (openChange)="!$event && svc.answer(false)" [title]="c.title">
        <p class="m-0">{{ c.message }}</p>
        @if (c.itemName) {<p class="m-0 mt-2 font-semibold">“{{ c.itemName }}”</p>}
        <div modalFooter class="contents">
          <button mButton variant="ghost" (click)="svc.answer(false)">{{ 'বাতিল' | tr }}</button>
          <button mButton variant="destructive" (click)="svc.answer(true)">{{ c.confirmLabel }}</button>
        </div>
      </m-modal>
    }
  `,
})
export class ConfirmHost {
  protected svc = inject(ConfirmService);
}
