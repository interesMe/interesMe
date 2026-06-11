import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { APP_ROUTES } from '../constants/routes.constants';
import { normalizeReturnUrl } from '../utils/return-url.util';
import { AuthStore } from '../../modules/auth/state/auth.store';

export const guestGuard: CanActivateFn = (route) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (!authStore.hasCompleteSession()) {
    authStore.clear();
    return true;
  }

  const returnUrl = route.queryParamMap.get('returnUrl');

  return returnUrl
    ? router.parseUrl(normalizeReturnUrl(returnUrl))
    : router.createUrlTree([APP_ROUTES.initiatives]);
};
