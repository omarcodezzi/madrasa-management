import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { PreferencesService } from '../../core/preferences.service';
import { SessionService } from '../../core/session.service';
import { Segmented } from '../../shared/ui';

/** Green panel with the star pattern and a mihrab arch (spec 2.3: pattern allowed on login). */
@Component({
  selector: 'app-auth-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, Segmented],
  host: { class: 'min-h-dvh grid lg:grid-cols-[minmax(0,1fr)_minmax(0,560px)] bg-bg' },
  template: `
    <aside class="relative bg-side text-[#e9f3ec] pattern overflow-hidden flex flex-col justify-center px-[clamp(24px,6vw,72px)] py-10 lg:py-16">
      <div class="relative z-10 max-w-md">
        <div class="mx-auto lg:mx-0 w-[150px] lg:w-[180px] aspect-[3/3.6] rounded-t-full border-[1.5px] border-[#e0a50f88] grid place-items-center mb-6 lg:mb-8">
          <div class="size-20 lg:size-24 rounded-2xl bg-white border-2 border-gold grid place-items-center text-g font-serif font-extrabold text-4xl">
            @if (session.profile()?.logoPath; as logo) { <img [src]="logo" alt="" class="size-full object-contain p-1.5"> } @else { م }
          </div>
        </div>
        <p class="text-[#f3cf6b] font-semibold m-0 text-center lg:text-left">{{ 'auth.welcome' | transloco }}</p>
        <h1 class="!text-white mt-2 text-center lg:text-left !text-[clamp(24px,3vw,34px)]">{{ session.profile()?.nameBn ?? ('app.name' | transloco) }}</h1>
        @if (session.profile()?.nameAr; as ar) {
          <p class="ar m-0 mt-2 text-[#f3cf6b] text-xl font-bold text-center lg:text-left" style="direction: rtl">{{ ar }}</p>
        }
        @if (session.profile()?.address; as addr) {
          <p class="hidden lg:block m-0 mt-6 text-[#bfe0cb] text-sm">{{ addr }}</p>
        }
      </div>
    </aside>
    <main class="flex flex-col px-[clamp(20px,5vw,56px)] py-8">
      <div class="flex justify-end">
        <m-segmented [options]="langs" [value]="prefs.lang()" (valueChange)="prefs.lang.set($event)" [attr.aria-label]="'shell.language' | transloco" />
      </div>
      <div class="flex-1 grid place-items-center py-8">
        <div class="w-full max-w-[400px]"><ng-content /></div>
      </div>
    </main>
  `,
})
export class AuthLayout {
  protected prefs = inject(PreferencesService);
  protected session = inject(SessionService);
  protected langs = [{ value: 'bn' as const, label: 'বাংলা' }, { value: 'en' as const, label: 'English' }];
}
