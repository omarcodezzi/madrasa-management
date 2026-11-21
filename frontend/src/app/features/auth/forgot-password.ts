import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { DigitsPipe } from '../../core/format';
import { SessionService } from '../../core/session.service';
import { Button, Icon } from '../../shared/ui';
import { AuthLayout } from './auth-layout';

/**
 * No email/SMS gateway yet, so resets go through the office (an admin sets a new password in Settings → Users, Phase 6).
 * When SMS is configured this page becomes a one-time-code flow.
 */
@Component({
  selector: 'app-forgot-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoPipe, DigitsPipe, AuthLayout, Button, Icon],
  template: `
    <app-auth-layout>
      <div class="size-14 rounded-full bg-goldbg text-goldink grid place-items-center mb-4"><m-icon name="info" [size]="26" /></div>
      <h2 class="!text-[26px]">{{ 'auth.forgotTitle' | transloco }}</h2>
      <p class="m-0 mt-2 mb-6 text-mut">{{ 'auth.forgotDesc' | transloco }}</p>
      @if (session.profile()?.phone; as phone) {
        <a mButton size="lg" [block]="true" [href]="'tel:' + phone"><m-icon name="phone" /> {{ 'auth.callOffice' | transloco }} · {{ phone | digits }}</a>
      }
      <a mButton variant="ghost" [block]="true" routerLink="/login" class="mt-2"><m-icon name="chevronLeft" /> {{ 'auth.back' | transloco }}</a>
    </app-auth-layout>
  `,
})
export class ForgotPasswordPage {
  protected session = inject(SessionService);
}
