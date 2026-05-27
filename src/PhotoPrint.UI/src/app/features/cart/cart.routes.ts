import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/cart-page').then(m => m.CartPage),
  },
];
