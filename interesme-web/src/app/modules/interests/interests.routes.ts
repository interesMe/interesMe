import { Routes } from '@angular/router';

export const INTERESTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/interests-page/interests-page.component').then((component) => component.InterestsPageComponent),
  },
];
