import { Routes } from '@angular/router';

export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/profile/profile').then((component) => component.ProfileComponent),
  },
  {
    path: 'edit',
    loadComponent: () =>
      import('./pages/profile-edit/profile-edit').then((component) => component.ProfileEditComponent),
  },
];
