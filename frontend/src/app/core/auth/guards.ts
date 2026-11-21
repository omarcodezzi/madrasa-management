import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_r, state) => {
  const auth = inject(AuthService);
  return auth.isLoggedIn() || inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return !auth.isLoggedIn() || inject(Router).parseUrl(auth.homeUrl());
};

/** Route data: { perm: 'students.view' }. Users without it go to their home page. */
export const permGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const perm = route.data?.['perm'] as string | undefined;
  return auth.can(perm) || inject(Router).parseUrl(auth.homeUrl());
};
