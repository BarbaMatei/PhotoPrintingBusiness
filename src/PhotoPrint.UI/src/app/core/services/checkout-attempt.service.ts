import { Injectable, inject } from '@angular/core';
import { AuthService } from './auth.service';

export const CHECKOUT_ATTEMPT_STORAGE_KEY = 'fotoTipar_checkoutAttempt';

// Matches the server's 24 h replay window; past it the same key creates a second order.
const ATTEMPT_TTL_MS = 24 * 60 * 60 * 1000;

interface StoredAttempt {
  key: string;
  owner: string;
  createdAt: number;
  orderId?: string;
  retired?: boolean;
}

function mintKey(): string {
  const c = globalThis.crypto as Crypto | undefined;
  if (typeof c?.randomUUID === 'function') return c.randomUUID();

  // randomUUID exists only in a secure context, so a plain-http deployment needs a fallback.
  const bytes = new Uint8Array(16);
  if (typeof c?.getRandomValues === 'function') {
    c.getRandomValues(bytes);
  } else {
    for (let i = 0; i < bytes.length; i++) bytes[i] = Math.floor(Math.random() * 256);
  }
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

function fingerprint(identity: string): string {
  let h = 0x811c9dc5;
  for (let i = 0; i < identity.length; i++) {
    h ^= identity.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  return (h >>> 0).toString(16);
}

@Injectable({ providedIn: 'root' })
export class CheckoutAttemptService {
  private readonly auth = inject(AuthService);

  private fallback: StoredAttempt | null = null;
  private storageWarned = false;

  idempotencyKey(): string {
    const owner = this.owner();
    const stored = this.read();
    if (stored && stored.owner === owner && this.isLive(stored) && !stored.retired) return stored.key;

    const attempt: StoredAttempt = { key: mintKey(), owner, createdAt: Date.now() };
    this.write(attempt);
    return attempt.key;
  }

  markOrderCreated(orderId: string): void {
    const stored = this.read();
    const attempt: StoredAttempt =
      stored && this.isLive(stored)
        ? { ...stored, orderId }
        : { key: mintKey(), owner: this.owner(), createdAt: Date.now(), orderId };
    this.write(attempt);
  }

  // Called once the card is confirmed: the order id stays so the confirmation page can still
  // wait on it, but the key is spent and the next basket must mint its own.
  retireKey(): void {
    const stored = this.read();
    if (!stored) return;
    this.write({ ...stored, retired: true });
  }

  isWaitingFor(orderId: string): boolean {
    const stored = this.read();
    return !!stored && stored.orderId === orderId && this.isLive(stored);
  }

  clear(): void {
    this.fallback = null;
    try {
      localStorage.removeItem(CHECKOUT_ATTEMPT_STORAGE_KEY);
    } catch {
      /* ignore */
    }
  }

  // Hashed because the guest token is a credential and must not be stored a second time.
  private owner(): string {
    const identity = this.auth.isAuthenticated()
      ? `u:${this.auth.currentUserId() ?? 'unknown'}`
      : `g:${this.auth.getGuestToken() ?? 'anon'}`;
    return fingerprint(identity);
  }

  private isLive(attempt: StoredAttempt): boolean {
    return Date.now() - attempt.createdAt < ATTEMPT_TTL_MS;
  }

  private read(): StoredAttempt | null {
    try {
      const raw = localStorage.getItem(CHECKOUT_ATTEMPT_STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as StoredAttempt;
        if (typeof parsed?.key === 'string' && typeof parsed?.createdAt === 'number') return parsed;
      }
    } catch {
      /* ignore */
    }
    return this.fallback;
  }

  private write(attempt: StoredAttempt): void {
    this.fallback = attempt;
    try {
      localStorage.setItem(CHECKOUT_ATTEMPT_STORAGE_KEY, JSON.stringify(attempt));
    } catch {
      if (!this.storageWarned) {
        this.storageWarned = true;
        console.warn(
          'Nu s-a putut salva încercarea de plată; protecția la dublă comandă durează doar cât pagina.',
        );
      }
    }
  }
}
