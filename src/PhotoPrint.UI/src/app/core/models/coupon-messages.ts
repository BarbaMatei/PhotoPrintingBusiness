import { HttpErrorResponse } from '@angular/common/http';

const MESSAGES: Record<string, string> = {
  INVALID_COUPON: 'Codul introdus nu este valid sau a expirat.',
  MIN_SUBTOTAL_NOT_MET: 'Codul se aplică doar la comenzi de o valoare mai mare.',
  COUPON_EXHAUSTED: 'Codul a atins limita de utilizări.',
  EMPTY_CART: 'Coșul este gol.',
  ORDER_TOTAL_BELOW_MINIMUM:
    'După reducere, valoarea comenzii este prea mică pentru a fi plătită online. ' +
    'Adaugă produse sau elimină codul.',
  NO_DISCOUNT: 'Codul nu produce nicio reducere pentru această comandă.',
};

const DEFAULT_MESSAGE = 'Codul introdus nu poate fi folosit.';

const RATE_LIMITED_MESSAGE =
  'Prea multe încercări. Așteaptă un minut înainte de a încerca din nou.';

const DETAIL_CARRIES_DATA = new Set([ 'MIN_SUBTOTAL_NOT_MET' ]);

/** Romanian sentence for a coupon error code, ignoring any server text. */
export function couponMessageFor(code: string | null | undefined): string {
  if (!code) return DEFAULT_MESSAGE;
  return MESSAGES[code] ?? DEFAULT_MESSAGE;
}

/**
 * Romanian sentence for a failed coupon call. Resolution is map-first: the
 * server sentence is used only for the code whose text carries the RON
 * threshold, which no other response exposes.
 */
export function couponErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 429) return RATE_LIMITED_MESSAGE;

  const body = (error.error ?? {}) as { code?: unknown; detail?: unknown };
  const code = typeof body.code === 'string' ? body.code : null;
  const detail = typeof body.detail === 'string' && body.detail.trim().length > 0
    ? body.detail.trim()
    : null;

  if (code && DETAIL_CARRIES_DATA.has(code) && detail) return detail;
  if (code && MESSAGES[code]) return MESSAGES[code];
  return detail ?? DEFAULT_MESSAGE;
}
