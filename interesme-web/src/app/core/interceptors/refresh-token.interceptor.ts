import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, Observable, switchMap, throwError } from 'rxjs';

import { APP_ROUTES } from '../constants/routes.constants';
import { TokenService } from '../services/token.service';
import { AuthService } from '../../modules/auth/services/auth.service';
import { BYPASS_REFRESH } from './http-context.tokens';

let refreshRequest$: Observable<string> | null = null;

export const refreshTokenInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(BYPASS_REFRESH)) {
    return next(request);
  }

  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !tokenService.getRefreshToken()) {
        return throwError(() => error);
      }

      refreshRequest$ ??= authService.refreshSession().pipe(finalize(() => (refreshRequest$ = null)));

      return refreshRequest$.pipe(
        switchMap((accessToken) =>
          next(
            request.clone({
              setHeaders: {
                Authorization: `Bearer ${accessToken}`,
              },
            }),
          ),
        ),
        catchError((refreshError: unknown) => {
          authService.clearSession();
          void router.navigate([APP_ROUTES.login]);
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
