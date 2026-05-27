import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

const GUEST_TOKEN_HEADER = 'X-Guest-Token';

/**
 * Attaches the guest session token to API requests when the user is not
 * authenticated but has a guest token (e.g. during checkout as a guest).
 */
export const guestInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Skip if authenticated (JWT interceptor handles that) or not an API call
  if (auth.isAuthenticated() || !req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  const guestToken = auth.getGuestToken();
  if (!guestToken) {
    return next(req);
  }

  return next(
    req.clone({ setHeaders: { [GUEST_TOKEN_HEADER]: guestToken } }),
  );
};
