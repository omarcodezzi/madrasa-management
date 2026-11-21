import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export interface SegOption<T = string> { value: T; label: string; }

/** Tabs/segmented control from the prototype (soft track, white active pill). */
@Component({
  selector: 'm-segmented',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex bg-soft rounded-xl p-1 gap-1', role: 'tablist' },
  template: `
    @for (o of options(); track o.value) {
      <button type="button" role="tab" [attr.aria-selected]="o.value === value()" (click)="value.set(o.value)"
        class="border-0 px-4 min-h-10 rounded-[9px] font-semibold cursor-pointer transition-colors duration-150"
        [class]="o.value === value() ? 'bg-card text-g2 shadow-sm' : 'bg-transparent text-mut hover:text-ink'">{{ o.label }}</button>
    }
  `,
})
export class Segmented<T> {
  readonly options = input.required<SegOption<T>[]>();
  readonly value = model.required<T>();
}

/** Filter chips: rounded pills, green when on. */
@Component({
  selector: 'm-chips',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-wrap gap-1.5' },
  template: `
    @for (o of options(); track o.value) {
      <button type="button" [attr.aria-pressed]="o.value === value()" (click)="value.set(o.value)"
        class="border rounded-full px-3.5 min-h-[38px] font-medium cursor-pointer transition-colors duration-150"
        [class]="o.value === value() ? 'bg-g border-g text-white' : 'bg-card border-line text-ink hover:border-g2'">{{ o.label }}</button>
    }
  `,
})
export class Chips<T> {
  readonly options = input.required<SegOption<T>[]>();
  readonly value = model.required<T>();
}
