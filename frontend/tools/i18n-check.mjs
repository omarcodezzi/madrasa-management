// Checks screen text for the English language.
//  - every Bangla text in src/app goes through tr() or the `tr` pipe
//  - every such text has an English entry in public/i18n/en-text.json
//  - every Bangla message the API can send (backend/src, not seed data) has an English entry;
//    `$"{x} পাওয়া যায়নি"` becomes the template `{0} পাওয়া যায়নি` (see trApi in core/tr.ts)
// Usage: node tools/i18n-check.mjs [--list]   (--list prints the texts missing English, as JSON)
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const root = new URL('..', import.meta.url).pathname;
const app = join(root, 'src/app');
const BN = /[\u0980-\u09FF]/;
// Files whose Bangla is data, not screen text (digit/month tables, tests).
const ALLOW = [/\.spec\.ts$/, /core\/format\.ts$/, /core\/tr\.ts$/];

const files = [];
(function walk(d) {
  for (const f of readdirSync(d)) {
    const p = join(d, f);
    if (statSync(p).isDirectory()) walk(p);
    else if (/\.(ts|html)$/.test(f) && !ALLOW.some((r) => r.test(p))) files.push(p);
  }
})(app);

const unesc = (s) => s.replace(/\\\\'/g, "'").replace(/\\'/g, "'").replace(/\\"/g, '"').replace(/&quot;/g, '"');
const patterns = [
  /(?<![\w.])tr\(\s*'((?:\\.|[^'\\])*)'/g,
  /(?<![\w.])tr\(\s*"((?:\\.|[^"\\])*)"/g,
  /'((?:\\.|[^'\\])*)'\s*\|\s*tr\b/g,
  /"((?:\\.|[^"\\])*)"\s*\|\s*tr\b/g,
];

const ids = new Map();
const leftovers = [];
for (const p of files) {
  // A line ending in `i18n-ignore` holds Bangla data (stored values), not screen text.
  let s = readFileSync(p, 'utf8').split('\n').map((l) => (l.includes('i18n-ignore') ? '' : l)).join('\n');
  for (const re of patterns) {
    s = s.replace(re, (m, body) => {
      const id = unesc(body);
      if (BN.test(id)) { if (!ids.has(id)) ids.set(id, relative(root, p)); }
      return m.replace(body, '');
    });
  }
  const blank = (m) => m.replace(/[^\n]/g, ''); // keep line numbers
  s = s.replace(/\/\*[\s\S]*?\*\//g, blank).replace(/^\s*\/\/.*$/gm, '').replace(/<!--[\s\S]*?-->/g, blank);
  s.split('\n').forEach((line, i) => { if (BN.test(line)) leftovers.push(`${relative(root, p)}:${i + 1}: ${line.trim().slice(0, 140)}`); });
}

// API messages. Seed data and migrations are database values, so they are skipped.
const backend = join(root, '../backend/src');
const SKIP_CS = [/Migrations\//, /DbInitializer\.cs$/, /SeedData\.cs$/, /Digits\.cs$/, /\/(bin|obj)\//];
const csFiles = [];
(function walk(d) {
  for (const f of readdirSync(d)) {
    const p = join(d, f);
    if (statSync(p).isDirectory()) walk(p);
    else if (f.endsWith('.cs') && !SKIP_CS.some((r) => r.test(p))) csFiles.push(p);
  }
})(backend);
for (const p of csFiles) {
  readFileSync(p, 'utf8').split('\n').forEach((line) => {
    const t = line.trim();
    if (line.includes('i18n-ignore') || t.startsWith('//')) return;
    for (const m of line.matchAll(/\\"title\\":\\"([^\\]*)\\"/g)) if (BN.test(m[1]) && !ids.has(m[1])) ids.set(m[1], relative(root, p));
    for (const m of line.matchAll(/(\$?)"((?:\\.|[^"\\])*)"/g)) {
      let body = m[2];
      if (!BN.test(body) || body.startsWith('{\\"')) continue;
      if (m[1]) { let i = 0; body = body.replace(/\{(?!\{)[^{}]*(?:\{[^{}]*\}[^{}]*)*\}/g, () => `{${i++}}`); }
      body = body.replace(/\\"/g, '"');
      if (!ids.has(body)) ids.set(body, relative(root, p));
    }
  });
}

const en = JSON.parse(readFileSync(join(root, 'public/i18n/en-text.json'), 'utf8'));
const missing = [...ids.keys()].filter((k) => !(k in en));
const unused = Object.keys(en).filter((k) => !ids.has(k) && !k.startsWith('api:'));

if (process.argv.includes('--list')) { console.log(JSON.stringify(missing, null, 1)); process.exit(0); }
if (leftovers.length) console.log(`Bangla text not wrapped in tr (${leftovers.length}):\n  ` + leftovers.join('\n  '));
if (missing.length) console.log(`Missing English (${missing.length}):\n  ` + missing.map((k) => `${k}   [${ids.get(k)}]`).join('\n  '));
if (unused.length) console.log(`Unused English entries (${unused.length}):\n  ` + unused.join('\n  '));
console.log(`${ids.size} texts, ${Object.keys(en).length} English entries.`);
process.exit(leftovers.length || missing.length ? 1 : 0);
