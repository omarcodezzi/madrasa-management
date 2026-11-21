import { Routes } from '@angular/router';
import { authGuard, guestGuard, permGuard } from './core/auth/guards';
import { unsavedGuard } from './core/unsaved.guard';
import { ALL_NAV } from './layout/nav';

const placeholder = () => import('./features/placeholder/placeholder').then((m) => m.PlaceholderPage);

/** Modules built so far; every other nav item shows its placeholder until its phase. */
const BUILT: Record<string, () => Promise<unknown>> = {
  dashboard: () => import('./features/dashboard/dashboard-page').then((m) => m.DashboardPage),
  students: () => import('./features/students/student-list').then((m) => m.StudentListPage),
  admission: () => import('./features/students/student-form').then((m) => m.StudentFormPage),
  attendance: () => import('./features/attendance/attendance-page').then((m) => m.AttendancePage),
  fees: () => import('./features/fees/fees-page').then((m) => m.FeesPage),
  idcards: () => import('./features/idcards/idcards-page').then((m) => m.IdCardsPage),
};

export const routes: Routes = [
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login').then((m) => m.LoginPage), title: 'লগইন' },
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
        ...(n.key === 'admission' ? { canDeactivate: [unsavedGuard] } : {}),
      })),
      { path: 'students/:id', canActivate: [permGuard], data: { nav: 'students', perm: 'students.view' }, loadComponent: () => import('./features/students/student-profile').then((m) => m.StudentProfilePage) },
      { path: 'students/:id/edit', canActivate: [permGuard], canDeactivate: [unsavedGuard], data: { nav: 'students', perm: 'students.edit' }, loadComponent: BUILT['admission'] as never },
      { path: 'design-system', loadComponent: () => import('./features/design-system/design-system').then((m) => m.DesignSystemPage) },
    ],
  },
  { path: '**', redirectTo: '' },
];
