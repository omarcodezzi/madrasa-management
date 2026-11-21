import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Photo, or the first letter of the name. */
@Component({
  selector: 'm-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'inline-grid place-items-center rounded-full bg-soft text-g2 font-bold overflow-hidden shrink-0',
    '[style.width.px]': 'size()', '[style.height.px]': 'size()', '[style.font-size.px]': 'size() * 0.4',
  },
  template: `@if (src()) {<img [src]="src()" [alt]="name()" class="size-full object-cover">} @else {<span aria-hidden="true">{{ initial() }}</span><span class="sr-only">{{ name() }}</span>}`,
})
export class Avatar {
  readonly name = input('');
  readonly src = input<string | null>();
  readonly size = input(40);
  protected initial = computed(() => {
    // Skip honorific prefixes so "মোঃ রফিক" shows র, not ম
    const parts = this.name().replace(/^(মোঃ|মো\.|মুহাম্মাদ|মাওলানা)\s+/, '').trim();
    return Array.from(parts)[0] ?? '?';
  });
}
