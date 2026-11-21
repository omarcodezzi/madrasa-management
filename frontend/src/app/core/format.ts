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

export const MONTHS_BN = ['জানুয়ারি', 'ফেব্রুয়ারি', 'মার্চ', 'এপ্রিল', 'মে', 'জুন', 'জুলাই', 'আগস্ট', 'সেপ্টেম্বর', 'অক্টোবর', 'নভেম্বর', 'ডিসেম্বর'];
export const MONTHS_EN = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
export const DAYS_BN = ['রবিবার', 'সোমবার', 'মঙ্গলবার', 'বুধবার', 'বৃহস্পতিবার', 'শুক্রবার', 'শনিবার'];

/** Today's date in the madrasa's time zone, as YYYY-MM-DD. */
export function todayIso(tz = 'Asia/Dhaka'): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: tz, year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}
export function monthOf(iso: string) { return iso.slice(0, 7); }
export function addMonths(ym: string, n: number): string {
  const [y, m] = ym.split('-').map(Number);
  const d = new Date(Date.UTC(y, m - 1 + n, 1));
  return `${d.getUTCFullYear()}-${String(d.getUTCMonth() + 1).padStart(2, '0')}`;
}

/** "2026-11" → "নভেম্বর ২০২৬" */
export function monthName(ym: string | null | undefined, digits: 'bn' | 'en' = 'bn', lang: 'bn' | 'en' = 'bn'): string {
  if (!ym) return '';
  const [y, m] = ym.split('-').map(Number);
  const name = (lang === 'en' ? MONTHS_EN : MONTHS_BN)[m - 1] ?? ym;
  return `${name} ${formatNumber(y, digits, false)}`;
}

/** "2026-11-22" → "২২ নভেম্বর ২০২৬" */
export function formatDate(iso: string | null | undefined, digits: 'bn' | 'en' = 'bn', lang: 'bn' | 'en' = 'bn'): string {
  if (!iso) return '';
  const [y, m, d] = iso.slice(0, 10).split('-').map(Number);
  const name = (lang === 'en' ? MONTHS_EN : MONTHS_BN)[m - 1];
  return `${formatNumber(d, digits, false)} ${name} ${formatNumber(y, digits, false)}`;
}

/** Hijri date for display beside the Gregorian one (spec 7: both calendars). */
export function hijriDate(iso: string, lang: 'bn' | 'en' = 'bn'): string {
  try {
    const [y, m, d] = iso.split('-').map(Number);
    const s = new Intl.DateTimeFormat(lang === 'bn' ? 'bn-BD-u-ca-islamic-umalqura' : 'en-u-ca-islamic-umalqura', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' })
      .format(new Date(Date.UTC(y, m - 1, d)));
    return s.replace(/\s*(যুগ|AH)$/, '') + (lang === 'bn' ? ' হিজরি' : ' AH');
  } catch { return ''; }
}

/** "14:05:00" → "২:০৫" */
export function formatTime(t: string | null | undefined, digits: 'bn' | 'en' = 'bn'): string {
  if (!t) return '';
  const [h, m] = t.split(':').map(Number);
  const h12 = ((h + 11) % 12) + 1;
  const s = `${h12}:${String(m).padStart(2, '0')}`;
  return digits === 'bn' ? toBnDigits(s) : s;
}

@Pipe({ name: 'bdate', pure: false })
export class DatePipe implements PipeTransform {
  private prefs = inject(PreferencesService);
  transform(v: string | null | undefined) { return formatDate(v, this.prefs.digits(), this.prefs.lang()); }
}
@Pipe({ name: 'month', pure: false })
export class MonthPipe implements PipeTransform {
  private prefs = inject(PreferencesService);
  /** Also accepts a comma list ("2026-10, 2026-11"), as receipts carry. */
  transform(v: string | null | undefined) {
    return (v ?? '').split(',').map((m) => m.trim()).filter(Boolean).map((m) => monthName(m, this.prefs.digits(), this.prefs.lang())).join(', ');
  }
}
@Pipe({ name: 'btime', pure: false })
export class TimePipe implements PipeTransform {
  private prefs = inject(PreferencesService);
  transform(v: string | null | undefined) { return formatTime(v, this.prefs.digits()); }
}

/** One report cell as text, by column kind (used on screen, in print and in CSV). */
export function reportCell(v: unknown, kind: string, digits: 'bn' | 'en' = 'bn'): string {
  if (v === null || v === undefined || v === '') return '';
  switch (kind) {
    case 'money': return formatMoney(Number(v), digits);
    case 'num': return formatNumber(v as number, digits);
    case 'pct': { const n = Math.round(Number(v) * 10) / 10; const t = groupBd(n, Number.isInteger(n) ? 0 : 1); return (digits === 'bn' ? toBnDigits(t) : t) + '%'; }
    case 'date': return /^\d{4}-\d{2}-\d{2}/.test(String(v)) ? formatDate(String(v), digits) : String(v);
    case 'month': return /^\d{4}-\d{2}$/.test(String(v)) ? monthName(String(v), digits) : String(v);
    default: return digits === 'bn' ? String(v) : toEnDigits(String(v));
  }
}
