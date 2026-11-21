import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Translation, TranslocoLoader } from '@jsverse/transloco';

/** Loads /i18n/{lang}.json. All UI text lives in those files (spec 8.4). */
@Injectable({ providedIn: 'root' })
export class HttpTranslocoLoader implements TranslocoLoader {
  private http = inject(HttpClient);
  getTranslation(lang: string) {
    return this.http.get<Translation>(`i18n/${lang}.json`);
  }
}

export const LANGS = ['bn', 'en'] as const;
export type Lang = (typeof LANGS)[number];
