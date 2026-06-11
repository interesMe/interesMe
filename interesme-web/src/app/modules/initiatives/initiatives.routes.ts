import { Routes } from '@angular/router';

import { authGuard } from '../../core/guards/auth-guard';

export const INITIATIVES_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/initiatives-list/initiatives-list').then((component) => component.InitiativesListPage),
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./pages/create-initiative/create-initiative').then((component) => component.CreateInitiativePage),
      },
      {
        path: ':id/apply',
        loadComponent: () =>
          import('./pages/apply-to-initiative/apply-to-initiative').then(
            (component) => component.ApplyToInitiativePage,
          ),
      },
      {
        path: ':id/applications',
        loadComponent: () =>
          import('./pages/applications-review/applications-review').then(
            (component) => component.ApplicationsReviewPage,
          ),
      },
      {
        path: ':id/edit',
        loadComponent: () =>
          import('./pages/edit-initiative/edit-initiative').then((component) => component.EditInitiativePage),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./pages/initiative-details/initiative-details').then(
            (component) => component.InitiativeDetailsPage,
          ),
      },
    ],
  },
];
