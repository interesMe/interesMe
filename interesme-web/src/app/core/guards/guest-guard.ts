import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { OnboardingRedirectService } from '../services/onboarding-redirect.service';
import { TokenService } from '../services/token.service';
import { AuthService } from '../../modules/auth/services/auth.service';
import { AuthStore } from '../../modules/auth/state/auth.store';

export const guestGuard: CanActivateFn = (route) => {
  const authStore = inject(AuthStore);
  const authService = inject(AuthService);
  const tokenService = inject(TokenService);
  const router = inject(Router);
  const onboardingRedirect = inject(OnboardingRedirectService);
  const hasCompleteSession = authStore.hasCompleteSession();
  const tokenServiceHasCompleteSession = tokenService.hasCompleteSession();

  console.debug('[guestGuard]', {
    url: router.url,
    returnUrl: route.queryParamMap.get('returnUrl'),
    isAuthenticated: authStore.isAuthenticated(),
    hasCompleteSession,
    tokenServiceHasCompleteSession,
  });

  const returnUrl = route.queryParamMap.get('returnUrl');
  const authenticatedRedirect = router.parseUrl(onboardingRedirect.getPostAuthRedirectUrl(returnUrl));

  return authService.validateSession().pipe(map((isValidSession) => (isValidSession ? authenticatedRedirect : true)));
};
