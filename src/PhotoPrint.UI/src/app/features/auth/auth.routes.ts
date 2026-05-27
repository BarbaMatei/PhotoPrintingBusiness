import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register-page').then(m => m.RegisterPage),
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login-page').then(m => m.LoginPage),
  },
  {
    path: 'verify-email',
    loadComponent: () =>
      import('./pages/verify-email/verify-email-page').then(m => m.EmailVerificationPendingPage),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password/forgot-password-page').then(m => m.ForgotPasswordPage),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./pages/reset-password/reset-password-page').then(m => m.ResetPasswordPage),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },
];
