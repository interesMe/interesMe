import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';

import { APP_ROUTES } from '../constants/routes.constants';
import { AuthStore } from '../../modules/auth/state/auth.store';

export const authGuard: CanActivateFn | CanActivateChildFn = (_route, state) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (authStore.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree([APP_ROUTES.login], {
    queryParams: {
      returnUrl: state.url,
    },
  });
};
