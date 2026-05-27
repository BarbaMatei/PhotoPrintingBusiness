import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { guestOrAuthGuard } from './core/guards/guest-or-auth.guard';
import { NotFound } from './shared/components/not-found/not-found';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home/home-page').then(m => m.HomePage),
  },
  {
    path: 'preturi',
    loadComponent: () => import('./features/pricing/pricing-page').then(m => m.PricingPage),
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.routes),
  },
  {
    path: 'tipareste',
    loadChildren: () => import('./features/upload/upload.routes').then(m => m.routes),
  },
  {
    path: 'cos',
    loadChildren: () => import('./features/cart/cart.routes').then(m => m.routes),
  },
  {
    path: 'checkout',
    canActivate: [guestOrAuthGuard],
    loadChildren: () => import('./features/checkout/checkout.routes').then(m => m.routes),
  },
  {
    path: 'comanda/:orderId/confirmare',
    loadComponent: () =>
      import('./features/orders/pages/confirmation-page').then(m => m.ConfirmationPage),
  },
  {
    path: 'comenzile-mele',
    canActivate: [authGuard],
    loadChildren: () => import('./features/orders/orders.routes').then(m => m.routes),
  },
  {
    path: 'contul-meu',
    canActivate: [authGuard],
    loadChildren: () => import('./features/account/account.routes').then(m => m.routes),
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.routes),
  },
  {
    path: 'politica-de-confidentialitate',
    loadComponent: () => import('./features/legal/privacy-policy').then(m => m.PrivacyPolicyPage),
  },
  {
    path: 'termeni-si-conditii',
    loadComponent: () => import('./features/legal/terms').then(m => m.TermsPage),
  },
  {
    path: 'politica-cookie',
    loadComponent: () => import('./features/legal/cookie-policy').then(m => m.CookiePolicyPage),
  },
  {
    path: '**',
    component: NotFound,
  },
];
