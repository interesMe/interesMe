import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth-guard';
import { guestGuard } from './core/guards/guest-guard';
import { APP_ROUTES } from './core/constants/routes.constants';
import { AppLayout } from './shared/layouts/app-layout/app-layout';
import { AuthLayout } from './shared/layouts/auth-layout/auth-layout';

export const routes: Routes = [
  {
    path: '',
    redirectTo: APP_ROUTES.profile,
    pathMatch: 'full',
  },
  {
    path: APP_ROUTES.auth,
    component: AuthLayout,
    canActivate: [guestGuard],
    loadChildren: () => import('./modules/auth/auth.routes').then((routesFile) => routesFile.AUTH_ROUTES),
  },
  {
    path: '',
    component: AppLayout,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      {
        path: APP_ROUTES.profile,
        loadChildren: () => import('./modules/profile/profile.routes').then((routesFile) => routesFile.PROFILE_ROUTES),
      },
      {
        path: APP_ROUTES.matches,
        loadChildren: () => import('./modules/matches/matches.routes').then((routesFile) => routesFile.MATCHES_ROUTES),
      },
      {
        path: APP_ROUTES.chat,
        loadChildren: () => import('./modules/chat/chat.routes').then((routesFile) => routesFile.CHAT_ROUTES),
      },
    ],
  },
  {
    path: '**',
    redirectTo: APP_ROUTES.profile,
  },
];
