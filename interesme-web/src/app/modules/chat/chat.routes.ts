import { Component } from '@angular/core';
import { Routes } from '@angular/router';

@Component({
  standalone: true,
  template: '<p>Chat works!</p>',
})
class ChatPageComponent {}

export const CHAT_ROUTES: Routes = [
  {
    path: '',
    component: ChatPageComponent,
  },
];
