import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { APP_ROUTES } from '../constants/routes.constants';
import { AuthStore } from '../../modules/auth/state/auth.store';

export const guestGuard: CanActivateFn = () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (!authStore.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree([APP_ROUTES.profile]);
};
