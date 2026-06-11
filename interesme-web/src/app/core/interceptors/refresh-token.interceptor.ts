import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, Observable, shareReplay, switchMap, throwError } from 'rxjs';

import { APP_ROUTES } from '../constants/routes.constants';
import { TokenService } from '../services/token.service';
import { normalizeReturnUrl } from '../utils/return-url.util';
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
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      if (!tokenService.getRefreshToken()) {
        clearSessionAndRedirect(authService, router);
        return throwError(() => error);
      }

      refreshRequest$ ??= authService.refreshSession().pipe(
        shareReplay(1),
        finalize(() => (refreshRequest$ = null)),
      );

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
          clearSessionAndRedirect(authService, router);
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};

function clearSessionAndRedirect(authService: AuthService, router: Router): void {
  const returnUrl = normalizeReturnUrl(router.url);
  authService.clearSession();
  void router.navigate([APP_ROUTES.login], {
    queryParams: {
      returnUrl,
    },
  });
}
