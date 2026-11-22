// UI audit: every page at 375/768/1440px, light and dark; reports page overflow, console errors and axe-core violations.
// Needs the API (sample data) on :5080, ng serve on :4200, and axe-core installed at /tmp/axe (npm i axe-core).
// Run from a folder where playwright-core resolves: node tools/ui-audit.mjs
import { chromium } from 'playwright-core';
import fs from 'fs';
const axeSrc = fs.readFileSync('/tmp/axe/node_modules/axe-core/axe.min.js', 'utf8');
const b = await chromium.launch({ executablePath: '/usr/bin/google-chrome', args: ['--no-sandbox'] });
const base = 'http://localhost:4200';
const pages = ['dashboard','attendance','students','students/1','admission','admission/new','classes','hifz','exams','idcards','fees','accounts','staff','hostel','library','notices','reports','settings'];
const vps = { phone: { width: 375, height: 812 }, tablet: { width: 768, height: 1024 }, desk: { width: 1440, height: 900 } };
const out = [];
async function session(vp, theme, user='admin', pass='Admin12345', landing='**/dashboard') {
  const ctx = await b.newContext({ viewport: vp, deviceScaleFactor: 1 });
  await ctx.addInitScript(t => localStorage.setItem('mms.theme', t), theme);
  const p = await ctx.newPage(); p.errs = [];
  p.on('pageerror', e => p.errs.push('pageerror ' + e.message));
  p.on('console', m => m.type() === 'error' && p.errs.push(m.text().slice(0, 160)));
  await p.goto(base + '/login', { waitUntil: 'networkidle' });
  await p.fill('#u', user); await p.fill('#p', pass); await p.click('button[type=submit]'); await p.waitForURL(landing); await p.waitForTimeout(800);
  return p;
}
async function check(p, name, tag, doAxe) {
  p.errs.length = 0;
  await p.goto(base + '/' + name, { waitUntil: 'networkidle' }); await p.waitForTimeout(1000);
  const ov = await p.evaluate(() => {
    const W = document.documentElement.clientWidth, res = [];
    if (document.documentElement.scrollWidth > W + 1) res.push('PAGE scrollWidth ' + document.documentElement.scrollWidth);
    for (const el of document.querySelectorAll('body *')) {
      const r = el.getBoundingClientRect(); if (!r.width || r.right <= W + 1) continue;
      let a = el.parentElement, clipped = false;
      while (a) { const s = getComputedStyle(a); if (/(auto|scroll|hidden|clip)/.test(s.overflowX)) { clipped = true; break; } a = a.parentElement; }
      if (!clipped) res.push((el.tagName + '.' + [...el.classList].slice(0, 3).join('.')).slice(0, 60) + ' r=' + Math.round(r.right));
      if (res.length > 4) break;
    }
    return res;
  });
  let ax = [];
  if (doAxe) {
    await p.evaluate(axeSrc);
    const r = await p.evaluate(async () => (await axe.run(document, { resultTypes: ['violations'] })).violations.map(v => ({ id: v.id, impact: v.impact, n: v.nodes.length, t: v.nodes.slice(0, 3).map(n => n.target.join(' ') + ' :: ' + (n.failureSummary || '').split('\n').slice(1, 2).join('').slice(0, 120)) })));
    ax = r;
  }
  await p.screenshot({ path: `${process.env.OUT || '/tmp/p7'}/${tag}-${name.replace(/\//g, '_')}.png` });
  out.push({ tag, name, ov, errs: [...p.errs], ax });
}
for (const [vk, vp] of Object.entries(vps)) for (const theme of (vk === 'tablet' ? ['light'] : ['light', 'dark'])) {
  const p = await session(vp, theme); const tag = vk + '-' + theme;
  for (const n of pages) await check(p, n, tag, vk === 'desk' || (vk==='phone' && theme==='light'));
  const g = await session(vp, theme, 'guardian', 'Guardian123', '**/portal'); await check(g, 'portal', tag, vk === 'desk');
  await g.context().close(); await p.context().close();
  const s = await (await b.newContext({ viewport: vp })).newPage(); s.errs = []; s.on('console', m => m.type() === 'error' && s.errs.push(m.text().slice(0,160)));
  await s.addInitScript(t => localStorage.setItem('mms.theme', t), theme);
  for (const n of ['site', 'login']) await check(s, n, tag, vk === 'desk');
  await s.context().close();
}
fs.writeFileSync((process.env.OUT || '/tmp/p7') + '/audit.json', JSON.stringify(out, null, 1));
await b.close();
