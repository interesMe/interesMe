import { Routes } from '@angular/router';

export const PUBLIC_INITIATIVES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/public-initiative/public-initiative').then((component) => component.PublicInitiativePage),
  },
];
