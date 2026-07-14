import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface GuestSessionData {
  guestToken: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
}

export interface CreateGuestSessionRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
}

const STORAGE_KEY = 'guestSession';

@Injectable({ providedIn: 'root' })
export class GuestAuthService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/auth`;

  createGuestSession(dto: CreateGuestSessionRequest): Observable<{ guestToken: string }> {
    return this.http.post<{ guestToken: string }>(`${this.base}/guest`, dto);
  }

  /** Creates an anonymous pre-session (no contact info). Used before the user
   * has entered their details, so they can upload photos immediately. */
  initAnonymousSession(): Observable<{ guestToken: string }> {
    return this.http.post<{ guestToken: string }>(`${this.base}/guest/init`, {});
  }

  /** Fills in contact info on an existing anonymous guest session at checkout. */
  updateContactInfo(dto: CreateGuestSessionRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/guest/contact`, dto);
  }

  claimGuestSession(guestToken: string): Observable<void> {
    return this.http.post<void>(`${this.base}/guest/claim`, { guestToken });
  }

  /** Persists the guest session. Contact fields (name/email/phone) are MERGED, not blindly
   *  overwritten: an incoming empty value preserves any existing non-empty value, so the anonymous
   *  re-init self-heal — which carries only a fresh token and empty contact — can't wipe the
   *  checkout contact info clearGuestToken was fixed to preserve (F3, review 042-v8; counterpart to
   *  F2). A caller setting real contact info still overwrites, since those incoming values are
   *  non-empty. The fresh guestToken always wins. */
  storeSession(data: GuestSessionData): void {
    const existing = this.getStoredSession();
    const merged: GuestSessionData = existing
      ? {
          guestToken: data.guestToken,
          firstName: data.firstName || existing.firstName,
          lastName: data.lastName || existing.lastName,
          email: data.email || existing.email,
          phone: data.phone || existing.phone,
        }
      : data;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(merged));
  }

  getStoredSession(): GuestSessionData | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as GuestSessionData;
    } catch {
      return null;
    }
  }

  clearSession(): void {
    localStorage.removeItem(STORAGE_KEY);
  }
}
