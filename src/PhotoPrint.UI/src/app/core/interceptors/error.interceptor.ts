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
          auth.logout();
          router.navigateByUrl('/auth/login');
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
