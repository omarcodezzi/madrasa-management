import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'gold' | 'destructive';
export type ButtonSize = 'sm' | 'md' | 'lg';

const VARIANT: Record<ButtonVariant, string> = {
  primary: 'bg-g text-white hover:bg-[var(--g-hover)] border-transparent',
  secondary: 'bg-soft text-g2 hover:brightness-95 border-transparent',
  outline: 'bg-card text-ink border-line hover:border-g2 hover:text-g2',
  ghost: 'bg-transparent text-ink border-transparent hover:bg-soft',
  gold: 'bg-gold text-[#2b1f00] hover:brightness-95 border-transparent',
  destructive: 'bg-redbg text-red border-transparent hover:brightness-95',
};
const SIZE: Record<ButtonSize, string> = {
  sm: 'min-h-9 px-3 text-sm gap-1.5',
  md: 'min-h-11 px-4 gap-2',
  lg: 'min-h-12 px-6 text-[17px] gap-2',
};

/**
 * <button mButton variant="outline" [loading]="saving()">সংরক্ষণ</button>
 * Works on <a> too.
 */
@Component({
  selector: 'button[mButton], a[mButton]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'cls()',
    '[attr.aria-busy]': 'loading() || null',
    '[attr.disabled]': '(loading() || disabled()) ? "" : null',
  },
  template: `
    @if (loading()) {
      <span class="size-4 rounded-full border-2 border-current border-r-transparent animate-spin" aria-hidden="true"></span>
    }
    <ng-content />
  `,
})
export class Button {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly loading = input(false);
  readonly disabled = input(false);
  readonly block = input(false);
  protected cls = computed(
    () =>
      'inline-flex items-center justify-center rounded-[10px] border font-semibold whitespace-nowrap no-underline cursor-pointer ' +
      'transition-[background,border-color,color,filter] duration-150 disabled:opacity-55 disabled:cursor-not-allowed ' +
      VARIANT[this.variant()] + ' ' + SIZE[this.size()] + (this.block() ? ' w-full' : ''),
  );
}

/** Square icon-only button; always give it an aria-label. */
@Component({
  selector: 'button[mIconButton]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class:
      'inline-grid place-items-center size-11 rounded-[10px] border border-transparent text-mut hover:bg-soft hover:text-ink cursor-pointer transition-colors duration-150',
  },
  template: `<ng-content />`,
})
export class IconButton {}
