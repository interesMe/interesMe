import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { APP_ROUTES } from '../constants/routes.constants';
import { TokenService } from '../services/token.service';
import { AuthService } from '../../modules/auth/services/auth.service';
import { AuthStore } from '../../modules/auth/state/auth.store';

export const authGuard: CanActivateFn | CanActivateChildFn = (_route, state) => {
  const authStore = inject(AuthStore);
  const authService = inject(AuthService);
  const tokenService = inject(TokenService);
  const router = inject(Router);
  const hasCompleteSession = authStore.hasCompleteSession();
  const tokenServiceHasCompleteSession = tokenService.hasCompleteSession();

  console.debug('[authGuard]', {
    url: state.url,
    isAuthenticated: authStore.isAuthenticated(),
    hasCompleteSession,
    tokenServiceHasCompleteSession,
  });

  const loginTree = router.createUrlTree([APP_ROUTES.login], {
    queryParams: {
      returnUrl: state.url,
    },
  });

  return authService.validateSession().pipe(map((isValidSession) => (isValidSession ? true : loginTree)));
};
