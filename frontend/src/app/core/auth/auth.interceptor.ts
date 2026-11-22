import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API } from '../api';
import { AuthService } from './auth.service';

/** Adds the bearer token to API calls; an expired/invalid token sends the user back to login. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();
  const isApi = req.url.startsWith(API);
  if (token && isApi) req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  return next(req).pipe(
    catchError((e) => {
      if (isApi && e?.status === 401 && token && !req.url.endsWith('/auth/login')) auth.logout(true, true);
      return throwError(() => e);
    }),
  );
};
