import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { Lang } from './i18n';
import { setCodeDigits } from './format';

export type Theme = 'system' | 'light' | 'dark';
export type Digits = 'bn' | 'en';

const read = <T extends string>(key: string, fallback: T): T => {
  try { return (localStorage.getItem(key) as T) || fallback; } catch { return fallback; }
};
const write = (key: string, v: string) => { try { localStorage.setItem(key, v); } catch { /* private mode */ } };

/** UI preferences kept on the device: theme, language, digit style. */
@Injectable({ providedIn: 'root' })
export class PreferencesService {
  private doc = inject(DOCUMENT);
  private transloco = inject(TranslocoService);

  readonly theme = signal<Theme>(read<Theme>('mms.theme', 'system'));
  readonly lang = signal<Lang>(read<Lang>('mms.lang', 'bn'));
  readonly digits = signal<Digits>(read<Digits>('mms.digits', 'bn'));

  constructor() {
    effect(() => {
      const t = this.theme();
      const root = this.doc.documentElement;
      if (t === 'system') { delete root.dataset['theme']; write('mms.theme', ''); }
      else { root.dataset['theme'] = t; write('mms.theme', t); }
    });
    const startLang = this.lang();
    effect(() => {
      const l = this.lang();
      this.transloco.setActiveLang(l);
      this.doc.documentElement.lang = l;
      write('mms.lang', l);
      if (l !== startLang) {
        // Text written in code is translated once at load, so a switch reloads the
        // page. Digits follow the language; they can still be changed after.
        write('mms.digits', l);
        this.doc.defaultView?.location.reload();
      }
    });
    effect(() => { const d = this.digits(); setCodeDigits(d); write('mms.digits', d); });
  }

  /** Cycles system → light → dark. */
  toggleTheme() {
    const order: Theme[] = ['system', 'light', 'dark'];
    this.theme.set(order[(order.indexOf(this.theme()) + 1) % order.length]);
  }
}
