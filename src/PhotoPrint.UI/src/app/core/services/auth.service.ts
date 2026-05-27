import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  isAdmin: boolean;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  accountLinked?: boolean;
}

export interface RegisterResponse {
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/auth`;

  private readonly isAuthenticated$$ = new BehaviorSubject<boolean>(false);
  private readonly isAdmin$$ = new BehaviorSubject<boolean>(false);
  private readonly currentUser$$ = new BehaviorSubject<CurrentUser | null>(null);

  constructor() {
    this.tryRestoreSession();
  }

  /** On every page load, check whether a non-expired token is already in sessionStorage
   *  and restore the authenticated state without forcing the user to log in again. */
  private tryRestoreSession(): void {
    const token = sessionStorage.getItem('access_token');
    if (!token) return;
    try {
      const payload = JSON.parse(
        atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))
      );
      const exp: number | undefined = payload['exp'];
      if (exp && Math.floor(Date.now() / 1000) >= exp) {
        sessionStorage.removeItem('access_token');
        return;
      }
      this.isAuthenticated$$.next(true);
      const role: string | undefined =
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
        payload['role'];
      this.isAdmin$$.next(role === 'Admin');
    } catch {
      sessionStorage.removeItem('access_token');
    }
  }

  readonly isAuthenticated$ = this.isAuthenticated$$.asObservable();
  readonly isAdmin$ = this.isAdmin$$.asObservable();
  readonly currentUser$ = this.currentUser$$.asObservable();

  private returnUrl = '/tipareste';

  /** Synchronous read — use in guards and interceptors. */
  isAuthenticated(): boolean {
    return this.isAuthenticated$$.value;
  }

  /** Synchronous read — use in guards. */
  isAdmin(): boolean {
    return this.isAdmin$$.value;
  }

  /** Returns the JWT access token from sessionStorage. */
  getAccessToken(): string | null {
    return sessionStorage.getItem('access_token');
  }

  /** Returns the guest token from localStorage. */
  getGuestToken(): string | null {
    const raw = localStorage.getItem('guestSession');
    if (!raw) return null;
    try {
      return (JSON.parse(raw) as { guestToken: string }).guestToken ?? null;
    } catch {
      return null;
    }
  }

  /** Persists the URL the user tried to access before being redirected to login. */
  setReturnUrl(url: string): void {
    this.returnUrl = url;
  }

  getReturnUrl(): string {
    return this.returnUrl;
  }

  /** Stores the access token and updates authenticated state. */
  setAuthenticated(response: LoginResponse): void {
    sessionStorage.setItem('access_token', response.accessToken);
    this.isAuthenticated$$.next(true);
    const isAdmin = this.decodeRole(response.accessToken) === 'Admin';
    this.isAdmin$$.next(isAdmin);
  }

  /** Decodes the role claim from a JWT without verifying signature. */
  private decodeRole(token: string): string | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
      return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
          ?? payload['role']
          ?? null;
    } catch {
      return null;
    }
  }

  /** Clears session state. */
  logout(): void {
    sessionStorage.removeItem('access_token');
    this.isAuthenticated$$.next(false);
    this.isAdmin$$.next(false);
    this.currentUser$$.next(null);
    this.returnUrl = '/tipareste';
  }

  // ── HTTP methods ────────────────────────────────────────────────────────────

  register(dto: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    confirmPassword: string;
    phone?: string | null;
    gdprConsentAccepted: boolean;
  }): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.base}/register`, dto);
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/login`, { email, password }).pipe(
      tap(res => this.setAuthenticated(res)),
    );
  }

  googleLogin(idToken: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/google`, { idToken }).pipe(
      tap(res => this.setAuthenticated(res)),
    );
  }

  resendConfirmation(email: string): Observable<void> {
    return this.http.post<void>(`${this.base}/resend-confirmation`, { email });
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.base}/forgot-password`, { email });
  }

  resetPassword(dto: {
    userId: string;
    token: string;
    newPassword: string;
    confirmPassword: string;
  }): Observable<void> {
    return this.http.post<void>(`${this.base}/reset-password`, dto);
  }
}

