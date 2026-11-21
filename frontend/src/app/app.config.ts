import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideTransloco } from '@jsverse/transloco';
import { routes } from './app.routes';
import { HttpTranslocoLoader } from './core/i18n';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    provideHttpClient(withFetch()),
    provideTransloco({
      config: { availableLangs: ['bn', 'en'], defaultLang: 'bn', fallbackLang: 'bn', reRenderOnLangChange: true, prodMode: !isDevMode() },
      loader: HttpTranslocoLoader,
    }),
  ],
};
