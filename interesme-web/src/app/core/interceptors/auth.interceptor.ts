import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { TokenService } from '../services/token.service';
import { BYPASS_AUTH } from './http-context.tokens';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(BYPASS_AUTH)) {
    return next(request);
  }

  const accessToken = inject(TokenService).getAccessToken();

  if (!accessToken) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`,
      },
    }),
  );
};
