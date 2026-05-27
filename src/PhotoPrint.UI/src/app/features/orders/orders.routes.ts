import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/order-history-page').then(m => m.OrderHistoryPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/order-detail-page').then(m => m.OrderDetailPage),
  },
];
