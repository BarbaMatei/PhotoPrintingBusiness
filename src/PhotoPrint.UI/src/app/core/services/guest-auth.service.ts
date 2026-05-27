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

  storeSession(data: GuestSessionData): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
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
