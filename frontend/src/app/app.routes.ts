import { Routes } from '@angular/router';
import { authGuard, guestGuard, permGuard } from './core/auth/guards';
import { unsavedGuard } from './core/unsaved.guard';
import { ALL_NAV } from './layout/nav';

const studentForm = () => import('./features/students/student-form').then((m) => m.StudentFormPage);
const placeholder = () => import('./features/placeholder/placeholder').then((m) => m.PlaceholderPage);

/** Modules built so far; every other nav item shows its placeholder until its phase. */
const BUILT: Record<string, () => Promise<unknown>> = {
  dashboard: () => import('./features/dashboard/dashboard-page').then((m) => m.DashboardPage),
  students: () => import('./features/students/student-list').then((m) => m.StudentListPage),
  admission: () => import('./features/admissions/admissions-page').then((m) => m.AdmissionsPage),
  attendance: () => import('./features/attendance/attendance-page').then((m) => m.AttendancePage),
  fees: () => import('./features/fees/fees-page').then((m) => m.FeesPage),
  classes: () => import('./features/academic/classes-page').then((m) => m.ClassesPage),
  hifz: () => import('./features/hifz/hifz-page').then((m) => m.HifzPage),
  exams: () => import('./features/exams/exams-page').then((m) => m.ExamsPage),
  idcards: () => import('./features/idcards/idcards-page').then((m) => m.IdCardsPage),
  hostel: () => import('./features/hostel/hostel-page').then((m) => m.HostelPage),
  library: () => import('./features/library/library-page').then((m) => m.LibraryPage),
  notices: () => import('./features/notices/notices-page').then((m) => m.NoticesPage),
  reports: () => import('./features/reports/reports-page').then((m) => m.ReportsPage),
  settings: () => import('./features/settings/settings-page').then((m) => m.SettingsPage),
  portal: () => import('./features/portal/portal-page').then((m) => m.PortalPage),
  staff: () => import('./features/staff/staff-page').then((m) => m.StaffPage),
  accounts: () => import('./features/accounts/accounts-page').then((m) => m.AccountsPage),
};

export const routes: Routes = [
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login').then((m) => m.LoginPage), title: 'লগইন' },
  { path: 'site', loadComponent: () => import('./features/public/public-site').then((m) => m.PublicSite), title: 'মাদরাসা' },
  { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password').then((m) => m.ForgotPasswordPage) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      // Each module replaces its placeholder in the phase that builds it
      ...ALL_NAV.map((n) => ({
        path: n.path, canActivate: [permGuard], data: { nav: n.key, perm: n.perm }, loadComponent: (BUILT[n.key] ?? placeholder) as never,
        // Marks autosave: finish the pending save before leaving
        ...(n.key === 'exams' ? { canDeactivate: [(c: { leave(): Promise<boolean> }) => c.leave()] } : {}),
      })),
      { path: 'students/:id', canActivate: [permGuard], data: { nav: 'students', perm: 'students.view' }, loadComponent: () => import('./features/students/student-profile').then((m) => m.StudentProfilePage) },
      { path: 'admission/new', canActivate: [permGuard], canDeactivate: [unsavedGuard], data: { nav: 'admission', perm: 'students.edit' }, loadComponent: studentForm },
      { path: 'students/:id/edit', canActivate: [permGuard], canDeactivate: [unsavedGuard], data: { nav: 'students', perm: 'students.edit' }, loadComponent: studentForm },
      { path: 'design-system', loadComponent: () => import('./features/design-system/design-system').then((m) => m.DesignSystemPage) },
    ],
  },
  { path: '**', redirectTo: '' },
];
