import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { NumPipe } from '../../core/format';
import { ALL_NAV } from '../../layout/nav';
import { Button, EmptyState, PageHeader } from '../../shared/ui';

/** Stand-in for modules built in later phases; keeps every menu link working. */
@Component({
  selector: 'app-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoPipe, NumPipe, Button, EmptyState, PageHeader],
  template: `
    @if (item(); as n) {
      <m-page-header [title]="'nav.' + n.key | transloco" [description]="n.desc" />
      <div class="bg-card border border-line rounded-[var(--radius-card)]">
        <m-empty-state [title]="'placeholder.comingIn' | transloco: { phase: (n.phase | num) }" [description]="'placeholder.comingDesc' | transloco" [icon]="n.icon">
          <a mButton variant="outline" routerLink="/design-system">{{ 'placeholder.designSystem' | transloco }}</a>
        </m-empty-state>
      </div>
    }
  `,
})
export class PlaceholderPage {
  /** From route data via withComponentInputBinding */
  readonly nav = input.required<string>();
  protected item = computed(() => ALL_NAV.find((n) => n.key === this.nav()));
}
