import { HttpErrorResponse } from '@angular/common/http';
import { couponErrorMessage, couponMessageFor } from './coupon-messages';

function problem(status: number, code: string | null, detail?: string): HttpErrorResponse {
  return new HttpErrorResponse({
    status,
    error: { ...(code ? { code } : {}), ...(detail !== undefined ? { detail } : {}) },
  });
}

describe('couponErrorMessage', () => {
  it('renders the Romanian sentence for INVALID_COUPON from the code, not from the server text', () => {
    const message = couponErrorMessage(problem(422, 'INVALID_COUPON', 'Coupon not found.'));

    expect(message).toBe('Codul introdus nu este valid sau a expirat.');
  });

  it('renders the Romanian sentence for COUPON_EXHAUSTED from the code', () => {
    const message = couponErrorMessage(problem(422, 'COUPON_EXHAUSTED', 'Redemption limit reached.'));

    expect(message).toBe('Codul a atins limita de utilizări.');
  });

  it('prefers the server sentence for MIN_SUBTOTAL_NOT_MET, the only text carrying the RON threshold', () => {
    const message = couponErrorMessage(
      problem(422, 'MIN_SUBTOTAL_NOT_MET', 'Codul se aplică la comenzi de minimum 150,00 RON.'),
    );

    expect(message).toBe('Codul se aplică la comenzi de minimum 150,00 RON.');
  });

  it('falls back to the map for MIN_SUBTOTAL_NOT_MET when the server sends no detail', () => {
    const message = couponErrorMessage(problem(422, 'MIN_SUBTOTAL_NOT_MET'));

    expect(message).toBe('Codul se aplică doar la comenzi de o valoare mai mare.');
  });

  it('renders the rate-limit sentence for 429, which carries no JSON body at all', () => {
    const error = new HttpErrorResponse({ status: 429, error: 'Too many requests' });

    expect(couponErrorMessage(error)).toBe(
      'Prea multe încercări. Așteaptă un minut înainte de a încerca din nou.',
    );
  });

  it('falls back to the server detail for an unknown code', () => {
    const message = couponErrorMessage(problem(422, 'SOMETHING_NEW', 'Server explains itself.'));

    expect(message).toBe('Server explains itself.');
  });

  it('falls back to the default sentence when the body has neither code nor detail', () => {
    const message = couponErrorMessage(new HttpErrorResponse({ status: 500 }));

    expect(message).toBe('Codul introdus nu poate fi folosit.');
  });
});

describe('couponMessageFor', () => {
  it('maps a stale-coupon reason code to its Romanian sentence', () => {
    expect(couponMessageFor('COUPON_EXHAUSTED')).toBe('Codul a atins limita de utilizări.');
  });

  it('returns the default sentence for a missing reason', () => {
    expect(couponMessageFor(null)).toBe('Codul introdus nu poate fi folosit.');
  });
});
