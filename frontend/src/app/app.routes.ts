import { Routes } from '@angular/router';
import { authGuard, guestGuard, permGuard } from './core/auth/guards';
import { ALL_NAV } from './layout/nav';

const placeholder = () => import('./features/placeholder/placeholder').then((m) => m.PlaceholderPage);

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
      ...ALL_NAV.map((n) => ({ path: n.path, canActivate: [permGuard], data: { nav: n.key, perm: n.perm }, loadComponent: placeholder })),
      { path: 'students/:id', canActivate: [permGuard], data: { nav: 'students', perm: 'students.view' }, loadComponent: placeholder },
      { path: 'design-system', loadComponent: () => import('./features/design-system/design-system').then((m) => m.DesignSystemPage) },
    ],
  },
  { path: '**', redirectTo: '' },
];
