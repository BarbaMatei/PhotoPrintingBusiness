import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/checkout-shell').then(m => m.CheckoutShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'livrare' },
      {
        path: 'livrare',
        loadComponent: () => import('./pages/delivery-step').then(m => m.DeliveryStep),
      },
      {
        path: 'recapitulare',
        loadComponent: () => import('./pages/review-step').then(m => m.ReviewStep),
      },
      {
        path: 'plata',
        loadComponent: () => import('./pages/payment-step').then(m => m.PaymentStep),
      },
    ],
  },
];
