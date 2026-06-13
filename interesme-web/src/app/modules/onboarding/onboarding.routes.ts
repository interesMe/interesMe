import { Routes } from '@angular/router';

export const ONBOARDING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/interests/interests').then((component) => component.OnboardingInterestsPage),
  },
];
