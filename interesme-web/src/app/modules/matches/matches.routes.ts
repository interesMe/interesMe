import { Routes } from '@angular/router';

export const MATCHES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/people-search/people-search').then((component) => component.PeopleSearchPage),
  },
];
