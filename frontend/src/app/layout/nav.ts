import { P } from '../core/auth/auth.service';
import { IconName } from '../shared/ui';
import { tr } from '../core/tr';

export interface NavItem { path: string; key: string; icon: IconName; perm: string; phase: number; desc: string; }
export interface NavGroup { key: string; items: NavItem[]; }

/** Sidebar groups follow the prototype; items without permission are hidden (spec 4). */
export const NAV: NavGroup[] = [
  {
    key: 'daily',
    items: [
      { path: 'dashboard', key: 'dashboard', icon: 'home', perm: P.dashboard, phase: 4, desc: tr('আজকের হাজিরা, আদায়, বকেয়া ও জরুরি কাজ এক নজরে।') },
      { path: 'attendance', key: 'attendance', icon: 'scan', perm: P.attendance, phase: 4, desc: tr('QR স্ক্যান বা এক ট্যাপে হাজিরা নিন।') },
      { path: 'students', key: 'students', icon: 'users', perm: P.studentsView, phase: 4, desc: tr('শিক্ষার্থী খুঁজুন, প্রোফাইল দেখুন, তথ্য বদলান।') },
      { path: 'admission', key: 'admission', icon: 'form', perm: P.admission, phase: 4, desc: tr('নতুন ভর্তির আবেদন থেকে ভর্তি পর্যন্ত।') },
    ],
  },
  {
    key: 'academic',
    items: [
      { path: 'classes', key: 'classes', icon: 'calendar', perm: P.studentsView, phase: 5, desc: tr('বিভাগ, শ্রেণি, শাখা, কিতাব ও রুটিন।') },
      { path: 'hifz', key: 'hifz', icon: 'quran', perm: P.hifz, phase: 5, desc: tr('সবক, সাবকি, মানজিল ও ৩০ পারার অগ্রগতি।') },
      { path: 'exams', key: 'exams', icon: 'exam', perm: P.examsMarks, phase: 5, desc: tr('নম্বর এন্ট্রি, ফলাফল, মেধা তালিকা ও মার্কশিট।') },
      { path: 'idcards', key: 'idcards', icon: 'card', perm: P.studentsView, phase: 4, desc: tr('QR সহ আইডি কার্ড প্রিন্ট করুন।') },
    ],
  },
  {
    key: 'office',
    items: [
      { path: 'fees', key: 'fees', icon: 'wallet', perm: P.fees, phase: 4, desc: tr('বেতন আদায়, রসিদ ও বকেয়া তালিকা।') },
      { path: 'accounts', key: 'accounts', icon: 'coins', perm: P.accounts, phase: 6, desc: tr('আয়-ব্যয় খাতা, দান ও ফান্ডের হিসাব।') },
      { path: 'staff', key: 'staff', icon: 'user', perm: P.staff, phase: 6, desc: tr('শিক্ষক ও স্টাফের তথ্য, হাজিরা ও বেতন।') },
      { path: 'hostel', key: 'hostel', icon: 'bed', perm: P.hostel, phase: 6, desc: tr('রুম, সিট বরাদ্দ ও ছুটির রেজিস্টার।') },
      { path: 'library', key: 'library', icon: 'library', perm: P.library, phase: 6, desc: tr('বই, ইস্যু ও ফেরত।') },
      { path: 'notices', key: 'notices', icon: 'mega', perm: P.notices, phase: 6, desc: tr('নোটিশ বোর্ড, SMS ও ইভেন্ট।') },
      { path: 'reports', key: 'reports', icon: 'chart', perm: P.reports, phase: 6, desc: tr('হাজিরা, আদায়, বকেয়া ও ফলাফলের রিপোর্ট।') },
      { path: 'settings', key: 'settings', icon: 'gear', perm: P.settings, phase: 6, desc: tr('মাদরাসার তথ্য, সেশন, ইউজার ও রোল।') },
    ],
  },
  {
    key: 'portal',
    items: [{ path: 'portal', key: 'portal', icon: 'user', perm: P.portal, phase: 6, desc: tr('হাজিরা, হিফজ, ফলাফল ও বকেয়া।') }],
  },
];

export const ALL_NAV: NavItem[] = NAV.flatMap((g) => g.items);

/** Phone bottom bar (spec 3): Home, Students, Attendance, Fees, More */
export const BOTTOM_NAV = ['dashboard', 'students', 'attendance', 'fees'];
