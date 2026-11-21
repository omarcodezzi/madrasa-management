# মাদরাসা ম্যানেজমেন্ট সিস্টেম — Phase 1 প্ল্যান (রিভিউয়ের জন্য)

স্ট্যাক: **Angular** (ফ্রন্টএন্ড) · **.NET 8 Web API** (ব্যাকএন্ড) · **Microsoft SQL Server** (ডাটাবেস)

ডাটাবেস ডিজাইন: [`database/InitialCreate.sql`](../database/InitialCreate.sql) (EF Core migration থেকে তৈরি) — ৪৪টি টেবিল (লগইনের Identity টেবিল বাদে), ১৩টি অংশে ভাগ করা।

---

## ১. আর্কিটেকচার (Clean / Layered)

```
madrasa/
├── backend/
│   ├── Madrasa.Domain/          Entities, enums, business rules (no dependencies)
│   ├── Madrasa.Application/     Use cases, DTOs, validation (FluentValidation), interfaces
│   ├── Madrasa.Infrastructure/  EF Core + SQL Server, Identity, file storage, SMS, PDF
│   ├── Madrasa.Api/             ASP.NET Core Web API controllers, JWT auth, Swagger
│   └── Madrasa.Tests/           xUnit tests
├── frontend/                    Angular app
└── docker-compose.yml           SQL Server + API + web (one command to run locally)
```

**Backend**
- .NET 8 (LTS), ASP.NET Core Web API, EF Core 8 with migrations (code-first; `schema.sql` is the reviewed design)
- ASP.NET Core Identity + JWT access/refresh tokens; 9 roles; permissions as role claims (`students.view`, `fees.collect`…)
- FluentValidation (Bangla error messages), AutoMapper or Mapster, Serilog logging
- QuestPDF for receipts, mark sheets, ID cards, admission form (A4 + POS 80mm)
- ClosedXML for Excel export
- Background job (Hangfire or hosted service) for monthly invoice generation and SMS queue

**Frontend**
- Angular 18+ (standalone components, signals), TypeScript
- Tailwind CSS with the prototype's tokens (green `#0b5d33`, gold `#e0a50f`), own component library styled like the prototype
- Fonts: Noto Serif Bengali (headings), Hind Siliguri (body), Inter (numbers), Amiri (Arabic, `dir="rtl"`)
- i18n: Transloco (বাংলা default, English), Bangla/English digit setting
- Charts: ngx-echarts · Tables: Angular CDK table + own DataTable component · QR: html5-qrcode (scan) + qrcode (generate)
- Role guards on routes; menu items hidden by permission

---

## ২. টেবিল → স্ক্রিন ম্যাপ

| মডিউল | টেবিল | স্ক্রিন |
|---|---|---|
| সেটিংস | MadrasaProfile, AcademicSessions, AppSettings, GradeRules, FeeHeads, Funds | মাদরাসার তথ্য, সেশন, ইউজার ও রোল, ফি সেটিংস, গ্রেডিং, SMS, ভাষা, ব্যাকআপ |
| ইউজার ও রোল | AspNetUsers/Roles/RoleClaims | লগইন, পাসওয়ার্ড রিসেট, ইউজার তালিকা, রোল পারমিশন |
| বিভাগ ও শ্রেণি | Departments, Classes, Sections, Subjects, ClassSubjects, RoutineSlots | বিভাগ/শ্রেণি/শাখা, বিষয় ও কিতাব, সাপ্তাহিক রুটিন গ্রিড |
| শিক্ষার্থী | Students, Guardians, StudentGuardians, Enrollments, StudentDocuments | তালিকা (ফিল্টার, বাল্ক, এক্সপোর্ট), প্রোফাইল ট্যাব, মাল্টি-স্টেপ ফর্ম, আইডি কার্ড (QR) |
| ভর্তি | AdmissionApplications | আবেদন পাইপলাইন (আবেদন → পরীক্ষা → অনুমোদন → ভর্তি), ভর্তি ফর্ম প্রিন্ট |
| হাজিরা | StudentAttendance, StaffAttendance, Holidays | QR স্ক্যান + এক ট্যাপে হাজিরা, মাসিক ক্যালেন্ডার, ড্যাশবোর্ডের রোল-কল |
| হিফজ | HifzJuzProgress, HifzDailyLogs | ৩০ পারার গ্রিড, দৈনিক সবক/সবকি/মানযিল, ভুলের সংখ্যা, চার্ট |
| পরীক্ষা | Exams, ExamMarks, ExamResults, GradeRules | পরীক্ষা সেটআপ, মার্ক এন্ট্রি গ্রিড, মেধা তালিকা, মার্কশিট, ফলাফল প্রকাশ |
| বেতন ও হিসাব | FeeHeads, FeeInvoices, FeeReceipts, FeeReceiptLines, StudentDiscounts | ফি আদায় (খুঁজুন → বকেয়া → আদায় → রসিদ), বকেয়া তালিকা, ছাড়/বৃত্তি |
| আয়-ব্যয় ও দান | Funds, LedgerCategories, LedgerEntries, Donors | আয়-ব্যয় খাতা, দান (যাকাত/সদকা/লিল্লাহ/সাধারণ আলাদা), ফান্ড ব্যালেন্স |
| শিক্ষক ও স্টাফ | Staff, StaffAttendance, SalaryPayments, ClassSubjects | তালিকা, প্রোফাইল, ক্লাস/বিষয়, হাজিরা, বেতন |
| হোস্টেল | HostelRooms, HostelAllocations, HostelLeaves | রুম ও সিট, বরাদ্দ, ছুটির রেজিস্টার |
| লাইব্রেরি | Books, BookIssues | বই তালিকা, ইস্যু/ফেরত, মেয়াদোত্তীর্ণ |
| নোটিশ | Notices, Events, MessageLogs | নোটিশ বোর্ড, SMS/WhatsApp, ইভেন্ট ক্যালেন্ডার |
| রিপোর্ট | (সব টেবিল থেকে) | হাজিরা, বকেয়া, আদায়, আয়-ব্যয়, ফলাফল — PDF/Excel |
| পোর্টাল | Students, Guardians + উপরের সব | অভিভাবক/শিক্ষার্থী পোর্টাল, পাবলিক ওয়েবসাইট |

## ৩. প্রোটোটাইপের সাথে সামঞ্জস্য
- আইডি কার্ডের QR ফরম্যাট `{"type":"RMTC_STUDENT","id":"RM-2026-001"}` অপরিবর্তিত থাকবে। `Students.StudentCode` এই আইডি রাখে, তাই আগে প্রিন্ট করা কার্ড কাজ করবে।
- প্রোটোটাইপের localStorage ডেটা (`rmtc_students` ইত্যাদি) ইমপোর্ট করার জন্য একটি ওয়ান-টাইম ইমপোর্ট স্ক্রিন থাকবে।
- বেফাক বিভাজন (মুমতায / জায়্যিদ জিদ্দান / জায়্যিদ / মাকবুল / রাসিব) `GradeRules` টেবিলে থাকবে, সেটিংস থেকে বদলানো যাবে।

## ৪. আমার নেওয়া সিদ্ধান্ত (ভুল হলে বলবেন)
1. একজন শিক্ষার্থীর প্রতি সেশনে আলাদা Enrollment, যাতে শ্রেণি-ইতিহাস থাকে।
2. সব টাকার লেনদেন একটি `LedgerEntries` খাতায় যায়, প্রতিটি একটি ফান্ডে। এতে যাকাত/লিল্লাহ/সাধারণ ফান্ডের ব্যালেন্স কখনো মিশে যায় না।
3. সাপ্তাহিক ছুটি শুক্রবার (সেটিংসে বদলানো যাবে)।
4. পেমেন্ট মেথড: নগদ, বিকাশ, নগদ (Nagad), ব্যাংক। অনলাইন পেমেন্ট গেটওয়ে পরে।

## ৫. পরের ধাপগুলো
| ধাপ | কাজ |
|---|---|
| ২ | সলিউশন স্ট্রাকচার, EF Core মাইগ্রেশন, সিড ডেটা; Angular ডিজাইন সিস্টেম + `/design-system` পেজ |
| ৩ | লগইন, সাইডবার, টপবার, কমান্ড প্যালেট |
| ৪ | ড্যাশবোর্ড → শিক্ষার্থী → হাজিরা → ফি |
| ৫ | শ্রেণি/রুটিন → হিফজ → পরীক্ষা |
| ৬ | শিক্ষক, হোস্টেল, লাইব্রেরি, নোটিশ, রিপোর্ট, সেটিংস, পোর্টাল |
| ৭ | রেসপন্সিভ, ডার্ক মোড, প্রিন্ট টেমপ্লেট, অ্যাক্সেসিবিলিটি |

প্রতিটি ধাপ শেষে থেমে আপনার রিভিউ নেব।
