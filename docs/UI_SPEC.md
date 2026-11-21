# MASTER PROMPT — Madrasa Management System UI

> Paste this whole file into Antigravity (or save it in your project as `docs/UI_SPEC.md` and tell the agent: "Read docs/UI_SPEC.md and follow it strictly").
> Fill in the `[ ]` brackets in Section 0 before you start.

---

## 0. Project Context (FILL THIS IN)

- Project name: **[Your Madrasa / Product name]**
- Tech stack: **[e.g. Laravel + Blade / Next.js / React + Vite / Django]**
  - If I haven't decided on a frontend: use **Next.js (App Router) + TypeScript + Tailwind CSS + shadcn/ui + lucide-react icons + Recharts + TanStack Table + React Hook Form + Zod**.
- Database: already designed. **Before writing any UI, read my schema/migrations/models in `[path to schema]` and map every table to a screen.** Do not invent fields that don't exist; if a screen needs a field that's missing, list it for me instead of silently adding it.
- Primary language: **Bangla (বাংলা)**, secondary: English, plus Arabic text in places (Quran/Kitab names, Arabic subjects). Build i18n from day one.

---

## 0.1 Visual reference (IMPORTANT)

I have attached `madrasa-management.html` — a working prototype of the redesigned UI. **Treat it as the source of truth for look and feel**: deep madrasa green `#0b5d33` sidebar with a faint gold star pattern, gold `#e0a50f` accent, Noto Serif Bengali headings + Hind Siliguri body, the "today's roll call" hero on the dashboard (each student drawn as a token, filled when present), the ID card layout with QR (payload `{"type":"RMTC_STUDENT","id":"..."}` — keep this format so already printed cards keep working), 30-juz hifz grid, fee receipt (POS 80mm + A4), marks grid with automatic Befaq-style division (মুমতায / জায়্যিদ জিদ্দান / জায়্যিদ / মাকবুল / রাসিব).
Rebuild these screens as real components connected to my database, keep the same layout, wording and flows, and replace browser `localStorage` with real API calls. Where Section 2 colors differ from the prototype, **the prototype wins**.

---

## 1. Your Role

You are a senior product designer + senior frontend engineer who has shipped SaaS dashboards like Linear, Stripe Dashboard, Vercel and Notion. Build a **production-grade, professional, beautiful and very easy-to-use** UI for a Madrasa Management System. The users include non-technical people (Muhtamim/principal, teachers, accountants, parents) — so clarity and simplicity come before cleverness.

The result must feel like a modern premium SaaS product, **not** a generic admin template. Avoid the "Bootstrap admin theme from 2015" look: no heavy gradients everywhere, no cluttered cards, no random colors, no tiny cramped tables.

---

## 2. Design Direction

**Mood:** calm, trustworthy, dignified, modern-Islamic. Clean and spacious, with a subtle touch of Islamic identity — never kitsch.

### 2.1 Color system (define as CSS variables / Tailwind tokens, support light + dark mode)
| Token | Light | Use |
|---|---|---|
| `primary` | Deep emerald `#0F766E` (teal-700) | main buttons, active nav, links |
| `primary-foreground` | `#FFFFFF` | |
| `accent` | Soft gold `#C9A227` | highlights, badges, small details only (max ~5% of screen) |
| `background` | `#F8FAF9` | page background |
| `surface` / `card` | `#FFFFFF` | cards, tables |
| `muted` | `#F1F5F4` | secondary backgrounds |
| `border` | `#E3E8E6` | |
| `text` | `#0F172A` | headings/body |
| `text-muted` | `#64748B` | labels, helper text |
| `success` | `#16A34A` | paid, present, passed |
| `warning` | `#D97706` | due soon, late |
| `danger` | `#DC2626` | absent, overdue, failed |
| `info` | `#2563EB` | |

Dark mode: background `#0B1412`, surface `#111C1A`, primary slightly lighter (`#14B8A6`), keep gold accent.

### 2.2 Typography
- Bangla: **Hind Siliguri** (or Noto Sans Bengali) — body 15–16px, line-height 1.6 (Bangla needs more line height).
- English/numbers: **Inter**. Use tabular numbers for money, roll numbers and marks.
- Arabic: **Amiri** or **Noto Naskh Arabic**, with `dir="rtl"` on Arabic text blocks.
- Scale: 12 / 14 / 16 / 18 / 20 / 24 / 30 / 36. Headings semibold, not bold-everywhere.
- Support both Bangla digits (০১২৩) and English digits via a setting.

### 2.3 Visual details
- Border radius: 12px cards, 8px inputs/buttons, full-round for avatars/badges.
- Shadows: very soft (`0 1px 2px rgba(0,0,0,.04), 0 4px 12px rgba(0,0,0,.04)`). Prefer borders over heavy shadows.
- Spacing: 8px grid. Generous whitespace. Page padding 24–32px on desktop.
- Islamic identity: a **very subtle geometric (girih/star) pattern** at 3–5% opacity used only on the login page, the sidebar header, empty states and the public landing hero. Optional small arch (mihrab) shape for hero image frames. Never behind data tables.
- Icons: lucide-react, 1.5px stroke, consistent size (18–20px).
- Motion: subtle 150–200ms transitions; skeleton loaders; no flashy animations. Respect `prefers-reduced-motion`.

---

## 3. App Layout (Shell)

- **Left sidebar** (collapsible to icons, 264px → 72px): logo + madrasa name on top; grouped navigation with section labels; active item has soft primary background + left indicator; user card at bottom.
- **Top bar**: page title + breadcrumbs, global search (`Ctrl/⌘ + K` command palette to jump to any student/page/action), academic year/session switcher, language switcher (বাংলা / English), dark-mode toggle, notifications bell, profile menu.
- **Mobile**: sidebar becomes a drawer; key pages also get a bottom tab bar (Home, Students, Attendance, Fees, More). Tables become stacked cards on small screens.
- Every page follows the same structure: **Page header (title + short description + primary action button on the right) → filters/tabs → content**.

---

## 4. Roles & Permissions (UI must adapt per role)
Super Admin, Muhtamim/Principal, Admin/Office Staff, Teacher/Ustad, Accountant, Hostel Supervisor, Librarian, Student, Parent/Guardian.
- Hide menu items a role can't access (don't just disable them).
- Each role gets its own dashboard (see Section 6).
- Map these to the roles/permissions tables already in my database.

---

## 5. Modules & Screens

Build these modules (adjust to match my database; skip what doesn't exist and tell me):

1. **Dashboard** (role-based, see Section 6)
2. **Students** — list (search, filter by class/department/section/status, bulk actions, export), profile page with tabs (Overview, Academic, Attendance, Fees, Results, Hifz Progress, Hostel, Documents, Guardian), add/edit form as a multi-step wizard.
3. **Admission** — online/offline application, application review pipeline (Applied → Interview/Test → Approved → Admitted), admission test marks, printable admission form.
4. **Departments & Classes** — e.g. Nurani, Nazera, Hifzul Quran, Kitab (Ibtedayi → Dawra-e-Hadith); classes, sections, subjects/kitabs, routines (weekly timetable grid).
5. **Teachers & Staff** — list, profile, assigned classes/subjects, attendance, salary.
6. **Hifz Progress Tracking** (key differentiator — make it beautiful): per student, daily Sabak (new lesson), Sabqi (recent revision), Manzil (old revision); para/juz progress visual (30-juz grid or progress ring), mistakes count, teacher remarks, weekly/monthly progress chart.
7. **Attendance** — fast daily attendance screen: class selector → student grid with one-tap Present/Absent/Late/Leave, "mark all present" button, works great on a phone. Monthly calendar heatmap per student. Staff attendance too.
8. **Exams & Results** — exam setup, mark entry (spreadsheet-like grid with keyboard navigation), auto grade/division calculation, merit list, printable mark sheets & result cards, publish results.
9. **Fees & Accounts** — fee heads (admission, monthly, exam, hostel, etc.), fee collection screen (search student → see dues → collect → print/download receipt), due list with reminders, discounts/scholarships (yatim/poor student support), **income & expense ledger**, donations (Zakat, Sadaqah, Lillah, General — keep separate funds), reports.
10. **Hostel / Boarding** — rooms & seats, allocation, meal/boarding fees, leave register.
11. **Library** — books (Arabic/Bangla/Urdu kitabs), issue/return, overdue.
12. **Notices & Communication** — notice board, SMS/notification to guardians, events calendar.
13. **Reports** — prebuilt reports with filters + export to PDF/Excel + print-friendly layout.
14. **Settings** — madrasa profile & logo, academic sessions, users & roles, fee settings, grading system, SMS settings, language & number format, backup.
15. **Student / Parent portal** — simple, mobile-first: today's attendance, hifz progress, results, fee dues & payment history, notices.
16. **Public website / landing page** (optional, if in scope) — hero with madrasa name and subtle pattern, about, departments, teachers, notice board, admission CTA, gallery, contact, footer.

---

## 6. Dashboards

**Admin/Muhtamim dashboard:**
- Top row of 4 KPI cards: Total Students, Today's Attendance %, This Month's Fee Collection (vs target), Total Dues. Each with a small trend indicator.
- Charts: monthly income vs expense (bar), attendance trend last 30 days (line), students by department (donut or horizontal bar).
- Lists: recent admissions, top dues, upcoming events/exams, latest notices.
- Quick actions: Add Student, Take Attendance, Collect Fee, Publish Notice.

**Teacher dashboard:** today's classes, take-attendance shortcut, pending mark entry, hifz students list with today's sabak status.

**Accountant dashboard:** today's collection, dues summary, recent transactions, fund balances (Zakat/Lillah/General separately).

**Parent dashboard:** child selector (if multiple), attendance this month, hifz progress, latest result, fee dues with pay button.

---

## 7. Component Standards (build a reusable component library first)

- **Buttons**: primary, secondary, outline, ghost, destructive; sizes sm/md/lg; loading state.
- **Forms**: label above input, helper text, inline validation messages in Bangla, required marker, grouped sections with headings, sticky footer with Save/Cancel on long forms, autosave drafts for long forms, date picker that supports both Gregorian and Hijri display.
- **Data table** (most important component): sticky header, column sorting, search, filter chips, column visibility toggle, pagination with page size, row selection + bulk actions bar, row actions menu (⋯), status badges, zebra-free clean rows with hover, export (CSV/Excel/PDF), density toggle, responsive card view on mobile.
- **Status badges**: soft-colored pills (e.g. green bg-50 + text-700).
- **Empty states**: friendly illustration/pattern + one-line explanation + primary action ("No students yet — Add your first student").
- **Loading**: skeletons matching the final layout (not spinners for whole pages).
- **Errors**: clear message + retry button. Toast notifications for success/failure.
- **Confirm dialogs** for destructive actions, with the item name shown.
- **Avatar** with initials fallback.
- **Print templates**: ID card, admission form, fee receipt, mark sheet, result card, certificates — A4 print CSS with madrasa header/logo.

---

## 8. UX Rules (must follow)

1. Any common task must take ≤ 3 clicks (take attendance, collect fee, find a student).
2. Global search finds students by name, ID, roll, phone, or guardian phone.
3. Always show the current academic session clearly.
4. Use plain, respectful Bangla labels — no technical jargon. Keep all text in translation files.
5. Numbers: money formatted as `৳ ১২,৫০০` (or English digits per setting).
6. Never lose user input — warn before leaving unsaved forms.
7. Accessibility: WCAG AA contrast, visible focus rings, keyboard navigable, aria-labels on icon buttons, min 44px tap targets on mobile.
8. Performance: paginate/virtualize big tables, lazy-load charts, optimistic UI where safe.
9. Consistency: same spacing, same header pattern, same table component on every page.

---

## 9. How to Work (execution plan)

Work in phases and **stop after each phase to show me screenshots / let me review** before continuing:

1. **Phase 1 – Analysis:** read my database schema and existing code; give me a short table mapping DB tables → screens, and list any gaps.
2. **Phase 2 – Design system:** tokens (colors, fonts, spacing), light/dark theme, i18n setup (bn/en), base components (Button, Input, Select, Badge, Card, Table, Modal, Toast, Skeleton, EmptyState, PageHeader). Make a `/design-system` preview page showing all of them.
3. **Phase 3 – App shell:** sidebar, top bar, command palette, auth pages (login, forgot password) with the subtle Islamic pattern.
4. **Phase 4 – Core modules:** Dashboard → Students → Attendance → Fees.
5. **Phase 5 – Academic modules:** Classes/Routine → Hifz Progress → Exams & Results.
6. **Phase 6 – Others:** Teachers, Hostel, Library, Notices, Reports, Settings, Parent portal, public site.
7. **Phase 7 – Polish:** responsive check on 375px / 768px / 1440px, dark mode check, empty/loading/error states on every page, print templates, accessibility pass.

Use realistic Bangla sample data (names like আব্দুল্লাহ, মুহাম্মাদ হাসান, ফাতিমা; classes like হিফজ বিভাগ, নাজেরা, মিশকাত) while the API isn't connected — but wire everything to my real models/APIs where they exist.

**Quality bar:** before finishing each phase, review your own output against Sections 2, 7 and 8 and fix anything inconsistent. If something in this spec conflicts with my existing code or database, ask me instead of guessing.
