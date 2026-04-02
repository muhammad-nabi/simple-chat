import { Routes } from '@angular/router';

export const chatRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/chat-layout/chat-layout.component').then(m => m.ChatLayoutComponent),
  },
];
