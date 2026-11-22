import { isDevMode, Pipe, PipeTransform } from '@angular/core';
import { fmtDigits, MONTHS_BN, MONTHS_EN } from './format';

/**
 * Screen text. The Bangla wording is written in the code and is also the
 * lookup key: `tr('নতুন যোগ করুন')`. When the language is English, the text comes
 * from public/i18n/en-text.json. `{name}` placeholders are filled from `args`.
 *
 * The language is fixed for the life of the page: switching it reloads the app,
 * so text built in code (column titles, toasts, options) never goes stale.
 * Values from the database (names, class names, notes) are never translated.
 */
let dict: Record<string, string> = {};
let lang: 'bn' | 'en' = 'bn';
const missing = new Set<string>();

export function activeLang() { return lang; }

export function setTranslations(l: 'bn' | 'en', d: Record<string, string> = {}) {
  lang = l;
  dict = d;
  templates = null;
}

export function tr(text: string, args?: Record<string, unknown>): string {
  let out = text;
  if (lang === 'en') {
    const hit = dict[text];
    if (hit !== undefined) out = hit;
    else if (isDevMode() && !missing.has(text)) { missing.add(text); console.warn('[i18n] missing English text:', text); }
  }
  if (args) out = out.replace(/\{(\w+)\}/g, (m, k: string) => (k in args ? String(args[k] ?? '') : m));
  return out;
}

/**
 * Text that came from the API (error titles, field errors, scan results, report
 * headings). The API writes Bangla; built messages such as `{0} পাওয়া যায়নি` are
 * matched against the template keys in en-text.json, and each filled-in part is
 * translated too (`শিক্ষার্থী` → `Student`), with month names and digits.
 */
export function trApi(text: string | null | undefined): string {
  if (!text) return text ?? '';
  if (lang === 'bn') return text;
  if (dict[text] !== undefined) return dict[text];
  for (const t of apiTemplates()) {
    const m = t.re.exec(text);
    if (m) return dict[t.key].replace(/\{(\d+)\}/g, (_, i: string) => trPart(m[t.order.indexOf(+i) + 1] ?? ''));
  }
  return trPart(text);
}

function trPart(s: string): string {
  if (dict[s] !== undefined) return dict[s];
  let out = s;
  MONTHS_BN.forEach((b, i) => { out = out.split(b).join(MONTHS_EN[i]); });
  return fmtDigits(out);
}

let templates: { key: string; re: RegExp; order: number[] }[] | null = null;
function apiTemplates() {
  if (!templates) {
    const esc = (x: string) => x.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    templates = Object.keys(dict)
      .filter((k) => /\{\d+\}/.test(k))
      .map((key) => {
        const order: number[] = [];
        const src = key.split(/(\{\d+\})/).map((p) => {
          const m = /^\{(\d+)\}$/.exec(p);
          if (m) { order.push(+m[1]); return '(.+?)'; }
          return esc(p);
        }).join('');
        return { key, re: new RegExp('^' + src + '$'), order };
      })
      // Longer fixed text first, so "{0} আগেই উপস্থিত" wins over "{0} উপস্থিত".
      .sort((a, b) => b.key.replace(/\{\d+\}/g, '').length - a.key.replace(/\{\d+\}/g, '').length);
  }
  return templates;
}

@Pipe({ name: 'tr' })
export class TrPipe implements PipeTransform {
  transform(text: string | null | undefined, args?: Record<string, unknown>): string {
    return text == null ? '' : tr(text, args);
  }
}

/** Runs before the app is imported (main.ts): loads the English text when English is chosen. */
export async function loadTranslations() {
  let l: 'bn' | 'en' = 'bn';
  try { l = localStorage.getItem('mms.lang') === 'en' ? 'en' : 'bn'; } catch { /* private mode */ }
  if (l === 'bn') { setTranslations('bn'); return; }
  try {
    const res = await fetch('i18n/en-text.json');
    setTranslations('en', res.ok ? await res.json() : {});
  } catch {
    setTranslations('en');
  }
}
