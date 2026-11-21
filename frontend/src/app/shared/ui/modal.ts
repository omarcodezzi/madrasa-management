import { ChangeDetectionStrategy, Component, ElementRef, effect, input, model, viewChild } from '@angular/core';
import { Icon } from './icon';

/**
 * Native <dialog>: focus trap, Esc to close and backdrop come from the browser.
 * <m-modal [(open)]="show" title="..."> body <div modalFooter>buttons</div></m-modal>
 */
@Component({
  selector: 'm-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <dialog #dlg (close)="open.set(false)" (click)="onBackdrop($event)" [attr.aria-labelledby]="'dlg-t'"
      class="m-auto w-[min(560px,calc(100vw-24px))] max-h-[calc(100dvh-24px)] p-0 rounded-2xl border border-line bg-card text-ink shadow-2xl backdrop:bg-black/40 backdrop:backdrop-blur-[2px]"
      [style.width]="width()">
      <div class="flex items-center justify-between gap-3 px-5 py-4 border-b border-line">
        <h3 id="dlg-t">{{ title() }}</h3>
        <button type="button" class="size-10 grid place-items-center rounded-lg hover:bg-soft text-mut cursor-pointer" (click)="open.set(false)" aria-label="বন্ধ করুন">
          <m-icon name="x" />
        </button>
      </div>
      <div class="px-5 py-4 overflow-y-auto"><ng-content /></div>
      <div class="flex justify-end gap-2 px-5 py-4 border-t border-line empty:hidden"><ng-content select="[modalFooter]" /></div>
    </dialog>
  `,
})
export class Modal {
  readonly open = model(false);
  readonly title = input.required<string>();
  readonly width = input<string>();
  private dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  constructor() {
    effect(() => {
      const d = this.dlg().nativeElement;
      if (this.open() && !d.open) d.showModal();
      if (!this.open() && d.open) d.close();
    });
  }
  protected onBackdrop(e: MouseEvent) {
    if (e.target === this.dlg().nativeElement) this.open.set(false);
  }
}
