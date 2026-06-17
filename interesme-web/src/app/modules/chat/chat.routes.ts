import { Routes } from '@angular/router';

export const CHAT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/chat-list/chat-list').then((component) => component.ChatListPage),
  },
  {
    path: 'direct/:conversationId',
    loadComponent: () => import('./pages/chat-thread/chat-thread').then((component) => component.ChatThreadPage),
    data: { threadType: 'direct' },
  },
  {
    path: 'groups/:groupChatId',
    loadComponent: () => import('./pages/chat-thread/chat-thread').then((component) => component.ChatThreadPage),
    data: { threadType: 'group' },
  },
  {
    path: 'channels/:channelId',
    loadComponent: () => import('./pages/chat-thread/chat-thread').then((component) => component.ChatThreadPage),
    data: { threadType: 'channel' },
  },
];
