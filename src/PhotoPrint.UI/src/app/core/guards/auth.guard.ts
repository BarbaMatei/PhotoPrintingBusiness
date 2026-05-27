import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Protects routes that require an authenticated user.
 * Redirects to /auth/login and saves the return URL.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  // Guard against open redirects: only save relative URLs
  const returnUrl = state.url.startsWith('/') ? state.url : '/';
  auth.setReturnUrl(returnUrl);

  return router.createUrlTree(['/auth/login']);
};
