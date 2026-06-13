import { Routes } from '@angular/router';

export const ONBOARDING_ROUTES: Routes = [
  {
    path: 'interests',
    loadComponent: () =>
      import('./pages/interests/interests').then((component) => component.OnboardingInterestsPage),
  },
  {
    path: 'loading',
    loadComponent: () => import('./pages/loading/loading').then((component) => component.OnboardingLoadingPage),
  },
  {
    path: 'hope',
    loadComponent: () => import('./pages/hope/hope').then((component) => component.OnboardingHopePage),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'interests',
  },
];
