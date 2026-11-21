import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Label above the control, helper text below, inline error in Bangla.
 * <m-field label="নাম" [required]="true" [error]="err"><input class="ctl" id="name"></m-field>
 */
@Component({
  selector: 'm-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <label [attr.for]="for()" class="block text-sm font-semibold text-mut mb-1.5">
      {{ label() }} @if (required()) {<span class="text-red" aria-hidden="true">*</span>}
    </label>
    <ng-content />
    @if (error()) {
      <p class="m-0 mt-1.5 text-sm text-red" role="alert">{{ error() }}</p>
    } @else if (hint()) {
      <p class="m-0 mt-1.5 text-sm text-mut">{{ hint() }}</p>
    }
  `,
})
export class FormField {
  readonly label = input.required<string>();
  readonly for = input<string>();
  readonly hint = input<string>();
  readonly error = input<string | null>();
  readonly required = input(false);
}
