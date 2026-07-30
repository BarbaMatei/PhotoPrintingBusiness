import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { Router } from '@angular/router';

/**
 * Intercepts HTTP error responses and presents user-facing feedback:
 * - 401: logs out the user (token refresh deferred to Epic 1)
 * - 403: shows "Acces interzis" toast
 * - 5xx: shows "Eroare de server" toast
 * - Network error: shows connectivity warning toast
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 401) {
          if (auth.isAuthenticated()) {
            // A logged-in user's token expired or was revoked — log out and send to login.
            auth.logout();
            router.navigateByUrl('/auth/login');
          } else {
            // Unauthenticated (guest or anonymous): a 401 means the guest session is stale
            // or absent. Clear any stored token so the next attempt re-inits a fresh one;
            // never bounce a guest to a login page they have no account for. This
            // also covers the no-token/corrupt-token case (clearGuestToken is a safe no-op).
            auth.clearGuestToken();
          }
        } else if (err.status === 403) {
          toast.show('Acces interzis. Nu ai permisiunile necesare.', 'error');
        } else if (err.status >= 500) {
          toast.show('Eroare de server. Încearcă din nou mai târziu.', 'error');
        } else if (err.status === 0) {
          toast.show('Eroare de rețea. Verifică conexiunea la internet.', 'warning');
        }
      }
      return throwError(() => err);
    }),
  );
};
