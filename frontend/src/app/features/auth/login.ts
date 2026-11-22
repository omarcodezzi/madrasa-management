import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { errorMessage } from '../../core/api';
import { AuthService } from '../../core/auth/auth.service';
import { Button, FormField, Icon } from '../../shared/ui';
import { AuthLayout } from './auth-layout';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, TranslocoPipe, AuthLayout, Button, FormField, Icon],
  template: `
    <app-auth-layout>
      <h2 class="!text-[26px]">{{ 'auth.signInTitle' | transloco }}</h2>
      <p class="m-0 mt-1.5 mb-6 text-mut">{{ 'auth.signInDesc' | transloco }}</p>

      @if (expired() && !error()) {
        <div class="mb-4 flex gap-2.5 items-start px-4 py-3 rounded-xl bg-soft text-ink" role="status">
          <m-icon name="info" class="mt-0.5 text-g2" />
          <span>{{ 'auth.sessionEnded' | transloco }}</span>
        </div>
      }
      @if (error()) {
        <div class="mb-4 flex gap-2.5 items-start px-4 py-3 rounded-xl bg-redbg text-red" role="alert">
          <m-icon name="alert" class="mt-0.5" />
          <div><b class="block">{{ error() }}</b><span class="text-sm">{{ 'auth.locked' | transloco }}</span></div>
        </div>
      }

      <form class="grid gap-4" (ngSubmit)="submit()" novalidate>
        <m-field [label]="'auth.userName' | transloco" for="u" [required]="true" [error]="tried() && !userName().trim() ? ('common.required' | transloco) : null">
          <input id="u" class="ctl" name="u" autocomplete="username" autocapitalize="none" spellcheck="false"
            [ngModel]="userName()" (ngModelChange)="userName.set($event)" [attr.aria-invalid]="tried() && !userName().trim()" autofocus>
        </m-field>
        <m-field [label]="'auth.password' | transloco" for="p" [required]="true" [error]="tried() && !password() ? ('common.required' | transloco) : null">
          <div class="relative">
            <input id="p" class="ctl !pr-12" name="p" [type]="show() ? 'text' : 'password'" autocomplete="current-password"
              [ngModel]="password()" (ngModelChange)="password.set($event)" [attr.aria-invalid]="tried() && !password()">
            <button type="button" class="absolute right-1 top-1/2 -translate-y-1/2 size-10 grid place-items-center rounded-lg text-mut hover:text-ink cursor-pointer"
              (click)="show.set(!show())" [attr.aria-label]="(show() ? 'auth.hidePassword' : 'auth.showPassword') | transloco" [attr.aria-pressed]="show()">
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" aria-hidden="true">
                <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>
                @if (show()) { <path d="M4 4l16 16"/> }
              </svg>
            </button>
          </div>
        </m-field>
        <div class="flex justify-end -mt-1">
          <a routerLink="/forgot-password" class="text-sm font-semibold">{{ 'auth.forgot' | transloco }}</a>
        </div>
        <button mButton type="submit" size="lg" [block]="true" [loading]="busy()">{{ 'auth.signIn' | transloco }}</button>
      </form>
      <p class="mt-5 mb-0 text-center text-sm"><a routerLink="/site">মাদরাসার ওয়েবসাইট ও অনলাইন ভর্তি</a></p>
    </app-auth-layout>
  `,
})
export class LoginPage {
  private auth = inject(AuthService);
  private router = inject(Router);
  private t = inject(TranslocoService);
  readonly returnUrl = input<string>();
  readonly expired = input<string>();

  protected userName = signal('');
  protected password = signal('');
  protected show = signal(false);
  protected busy = signal(false);
  protected tried = signal(false);
  protected error = signal<string | null>(null);

  async submit() {
    this.tried.set(true);
    this.error.set(null);
    if (!this.userName().trim() || !this.password()) return;
    this.busy.set(true);
    try {
      await this.auth.login(this.userName().trim(), this.password());
      const back = this.returnUrl();
      // Only follow in-app paths
      await this.router.navigateByUrl(back && back.startsWith('/') && !back.startsWith('//') ? back : this.auth.homeUrl());
    } catch (e) {
      this.error.set(errorMessage(e, this.t.translate('auth.badCredentials')));
    } finally {
      this.busy.set(false);
    }
  }
}
