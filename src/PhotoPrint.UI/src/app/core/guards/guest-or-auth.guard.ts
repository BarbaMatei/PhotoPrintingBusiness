import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Allows the route if the user is authenticated OR has a guest session token.
 * Used for checkout — guests with items in cart can proceed.
 * Redirects to /auth/login only when neither condition is met.
 */
export const guestOrAuthGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated() || auth.getGuestToken() !== null) {
    return true;
  }

  const returnUrl = state.url.startsWith('/') ? state.url : '/';
  auth.setReturnUrl(returnUrl);

  return router.createUrlTree(['/auth/login']);
};
