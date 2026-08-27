import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import {
  CheckoutAttemptService,
  CHECKOUT_ATTEMPT_STORAGE_KEY,
} from './checkout-attempt.service';
import { AuthService } from './auth.service';

const UUID_SHAPE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

describe('CheckoutAttemptService', () => {
  let service: CheckoutAttemptService;
  let identity: { authenticated: boolean; userId: string | null; guestToken: string | null };

  beforeEach(() => {
    localStorage.clear();
    identity = { authenticated: false, userId: null, guestToken: 'guest-token-1' };

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => identity.authenticated,
            currentUserId: () => identity.userId,
            getGuestToken: () => identity.guestToken,
          },
        },
      ],
    });
    service = TestBed.inject(CheckoutAttemptService);
  });

  afterEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it('hands out one key for the whole attempt, so a remount or a second tab reuses it', () => {
    const first = service.idempotencyKey();

    expect(service.idempotencyKey()).toBe(first);
    expect(first).toMatch(UUID_SHAPE);
  });

  it('keeps the key inside the server key-length cap', () => {
    expect(service.idempotencyKey().length).toBeLessThanOrEqual(80);
  });

  it('reads the key another tab already stored instead of minting a rival one', () => {
    service.idempotencyKey();
    const stored = JSON.parse(localStorage.getItem(CHECKOUT_ATTEMPT_STORAGE_KEY)!);
    localStorage.setItem(
      CHECKOUT_ATTEMPT_STORAGE_KEY,
      JSON.stringify({ ...stored, key: 'key-from-the-other-tab' }),
    );

    expect(service.idempotencyKey()).toBe('key-from-the-other-tab');
  });

  it('keeps the key after the order exists, so going back and forward replays it', () => {
    const key = service.idempotencyKey();
    service.markOrderCreated('order-1');

    expect(service.idempotencyKey()).toBe(key);
  });

  it('remembers the order it created, and only that one', () => {
    service.idempotencyKey();
    service.markOrderCreated('order-1');

    expect(service.isWaitingFor('order-1')).toBe(true);
    expect(service.isWaitingFor('order-2')).toBe(false);
  });

  it('is not waiting for anything before an order was created', () => {
    service.idempotencyKey();

    expect(service.isWaitingFor('order-1')).toBe(false);
  });

  it('drops the attempt on clear, so the next checkout gets a new key', () => {
    const first = service.idempotencyKey();
    service.clear();

    expect(localStorage.getItem(CHECKOUT_ATTEMPT_STORAGE_KEY)).toBeNull();
    expect(service.idempotencyKey()).not.toBe(first);
  });

  it('refuses to reuse a key older than the server replay window', () => {
    service.idempotencyKey();
    const stored = JSON.parse(localStorage.getItem(CHECKOUT_ATTEMPT_STORAGE_KEY)!);
    localStorage.setItem(
      CHECKOUT_ATTEMPT_STORAGE_KEY,
      JSON.stringify({ ...stored, key: 'stale-key', createdAt: Date.now() - 25 * 60 * 60 * 1000 }),
    );

    expect(service.idempotencyKey()).not.toBe('stale-key');
  });

  it('ignores a corrupt stored attempt instead of sending garbage as the key', () => {
    localStorage.setItem(CHECKOUT_ATTEMPT_STORAGE_KEY, '{not json');

    expect(service.idempotencyKey()).toMatch(UUID_SHAPE);
  });

  it('mints a new key when the guest session is replaced', () => {
    const first = service.idempotencyKey();

    identity.guestToken = 'guest-token-2';

    expect(service.idempotencyKey()).not.toBe(first);
  });

  it('mints a new key when the guest signs in, because the server scopes keys per caller', () => {
    const guestKey = service.idempotencyKey();

    identity.authenticated = true;
    identity.userId = 'user-1';

    expect(service.idempotencyKey()).not.toBe(guestKey);
  });

  it('leaves the stored guest session untouched, and never nests the key inside it', () => {
    const guestSession = JSON.stringify({
      guestToken: 'guest-token-1',
      firstName: 'Ana',
      lastName: 'Pop',
      email: 'ana@example.com',
      phone: '0712345678',
    });
    localStorage.setItem('guestSession', guestSession);

    service.idempotencyKey();
    service.markOrderCreated('order-1');
    service.clear();

    expect(localStorage.getItem('guestSession')).toBe(guestSession);
  });

  it('still dedupes within the page when localStorage refuses to store the attempt', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('QuotaExceededError');
    });
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);

    const first = service.idempotencyKey();

    expect(service.idempotencyKey()).toBe(first);
    expect(warn).toHaveBeenCalled();
  });

  it('mints a usable key without crypto.randomUUID (an insecure-context browser)', () => {
    const original = crypto.randomUUID;
    (crypto as { randomUUID?: unknown }).randomUUID = undefined;
    try {
      const key = service.idempotencyKey();
      expect(key).toMatch(UUID_SHAPE);
      service.clear();
      expect(service.idempotencyKey()).not.toBe(key);
    } finally {
      (crypto as { randomUUID?: unknown }).randomUUID = original;
    }
  });
});
