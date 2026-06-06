import { Component } from '@angular/core';
import { Routes } from '@angular/router';

@Component({
  standalone: true,
  template: '<p>Matches works!</p>',
})
class MatchesPageComponent {}

export const MATCHES_ROUTES: Routes = [
  {
    path: '',
    component: MatchesPageComponent,
  },
];
