import { inject, Pipe, PipeTransform } from '@angular/core';
import { PreferencesService } from './preferences.service';

const BN = '০১২৩৪৫৬৭৮৯';

export function toBnDigits(s: string): string {
  return s.replace(/[0-9]/g, (d) => BN[+d]);
}
export function toEnDigits(s: string): string {
  return s.replace(/[০-৯]/g, (d) => String(BN.indexOf(d)));
}

/** Indian/Bangladeshi grouping: 1,23,456 */
export function groupBd(n: number, fraction = 0): string {
  return n.toLocaleString('en-IN', { minimumFractionDigits: fraction, maximumFractionDigits: fraction });
}

export function formatNumber(v: number | string | null | undefined, digits: 'bn' | 'en', group = true): string {
  if (v === null || v === undefined || v === '') return '';
  const n = typeof v === 'number' ? v : Number(toEnDigits(String(v)));
  const s = Number.isFinite(n) ? (group ? groupBd(n) : String(n)) : String(v);
  return digits === 'bn' ? toBnDigits(s) : s;
}

/** `৳ ১২,৫০০` (spec 8.5) — follows the digit setting. */
export function formatMoney(v: number | null | undefined, digits: 'bn' | 'en'): string {
  if (v === null || v === undefined) return '';
  return '৳ ' + formatNumber(Math.round(v), digits);
}

/** {{ 1234 | num }} → ১,২৩৪ (or 1,234). Impure so it follows the setting live. */
@Pipe({ name: 'num', pure: false })
export class NumPipe implements PipeTransform {
  private prefs = inject(PreferencesService);
  transform(v: number | string | null | undefined, group = true) {
    return formatNumber(v, this.prefs.digits(), group);
  }
}

@Pipe({ name: 'money', pure: false })
export class MoneyPipe implements PipeTransform {
  private prefs = inject(PreferencesService);
  transform(v: number | null | undefined) {
    return formatMoney(v, this.prefs.digits());
  }
}

/** Converts any digits in a string (dates, IDs, phone numbers) to the chosen style. */
@Pipe({ name: 'digits', pure: false })
export class DigitsPipe implements PipeTransform {
  private prefs = inject(PreferencesService);
  transform(v: unknown) {
    if (v === null || v === undefined) return '';
    const s = String(v);
    return this.prefs.digits() === 'bn' ? toBnDigits(s) : toEnDigits(s);
  }
}
