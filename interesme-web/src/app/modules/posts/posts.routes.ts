import { Routes } from '@angular/router';

export const POSTS_ROUTES: Routes = [
  {
    path: 'new',
    loadComponent: () =>
      import('./pages/create-post/create-post').then((component) => component.CreatePostPage),
  },
];
