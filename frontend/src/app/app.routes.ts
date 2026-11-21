import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'design-system' },
  { path: 'design-system', loadComponent: () => import('./features/design-system/design-system').then((m) => m.DesignSystemPage) },
  { path: '**', redirectTo: 'design-system' },
];
