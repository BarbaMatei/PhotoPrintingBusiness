import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

/**
 * Attaches the JWT Bearer token to all requests directed at the API.
 * Skips external services (Stripe, Google) by checking the request URL.
 */
export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getAccessToken();

  // Only attach to own API; skip external service calls
  if (!token || !req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  return next(
    req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }),
  );
};
