import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./shell/admin-shell').then(m => m.AdminShell),
    children: [
      {
        path: '',
        loadComponent: () => import('./pages/admin-page').then(m => m.AdminPage),
      },
      {
        path: 'comenzi',
        loadComponent: () => import('./pages/orders/admin-orders-page').then(m => m.AdminOrdersPage),
      },
      {
        path: 'comenzi/:orderId',
        loadComponent: () => import('./pages/order-detail/admin-order-detail-page').then(m => m.AdminOrderDetailPage),
      },
      {
        path: 'produse',
        loadComponent: () => import('./pages/products/admin-products-page').then(m => m.AdminProductsPage),
      },
      {
        path: 'stari-comenzi',
        loadComponent: () => import('./pages/state-machine/admin-state-machine-page').then(m => m.AdminStateMachinePage),
      },
    ],
  },
];
