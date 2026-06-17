import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth-guard';
import { guestGuard } from './core/guards/guest-guard';
import { APP_ROUTES } from './core/constants/routes.constants';
import { AppLayout } from './shared/layouts/app-layout/app-layout';
import { AuthLayout } from './shared/layouts/auth-layout/auth-layout';

export const routes: Routes = [
  {
    path: '',
    redirectTo: APP_ROUTES.initiatives,
    pathMatch: 'full',
  },
  {
    path: APP_ROUTES.publicInitiative,
    loadChildren: () =>
      import('./modules/public-initiatives/public-initiatives.routes').then(
        (routesFile) => routesFile.PUBLIC_INITIATIVES_ROUTES,
      ),
  },
  {
    path: APP_ROUTES.auth,
    component: AuthLayout,
    canActivate: [guestGuard],
    loadChildren: () => import('./modules/auth/auth.routes').then((routesFile) => routesFile.AUTH_ROUTES),
  },
  {
    path: APP_ROUTES.onboarding,
    canActivate: [authGuard],
    loadChildren: () =>
      import('./modules/onboarding/onboarding.routes').then((routesFile) => routesFile.ONBOARDING_ROUTES),
  },
  {
    path: '',
    component: AppLayout,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      {
        path: APP_ROUTES.home,
        loadChildren: () => import('./modules/home/home.routes').then((routesFile) => routesFile.HOME_ROUTES),
      },
      {
        path: APP_ROUTES.profile,
        loadChildren: () => import('./modules/profile/profile.routes').then((routesFile) => routesFile.PROFILE_ROUTES),
      },
      {
        path: APP_ROUTES.initiatives,
        loadChildren: () =>
          import('./modules/initiatives/initiatives.routes').then((routesFile) => routesFile.INITIATIVES_ROUTES),
      },
      {
        path: APP_ROUTES.myInitiatives,
        loadComponent: () =>
          import('./modules/initiatives/pages/my-initiatives/my-initiatives').then(
            (component) => component.MyInitiativesPage,
          ),
      },
      {
        path: APP_ROUTES.chat,
        loadChildren: () => import('./modules/chat/chat.routes').then((routesFile) => routesFile.CHAT_ROUTES),
      },
      {
        path: APP_ROUTES.settings,
        loadChildren: () => import('./modules/settings/settings.routes').then((routesFile) => routesFile.SETTINGS_ROUTES),
      },
    ],
  },
  {
    path: '**',
    redirectTo: APP_ROUTES.initiatives,
  },
];
