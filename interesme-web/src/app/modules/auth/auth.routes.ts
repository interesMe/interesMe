import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((component) => component.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register').then((component) => component.RegisterComponent),
  },
  {
    path: 'callback',
    loadComponent: () => import('./pages/callback/callback').then((component) => component.CallbackComponent),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },
];
