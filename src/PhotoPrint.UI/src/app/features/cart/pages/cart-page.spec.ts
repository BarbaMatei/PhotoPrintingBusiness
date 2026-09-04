import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { CartPage } from './cart-page';
import { AuthService } from '../../../core/services/auth.service';
import { UploadService } from '../../../core/services/upload.service';
import { CartResponseDto, CART_STORAGE_KEY } from '../../../core/models/cart.model';
import { environment } from '../../../../environments/environment';

const BASE = `${environment.apiUrl}/cart`;

const SUBTOTAL = 250;

function makeCart(overrides: Partial<CartResponseDto> = {}): CartResponseDto {
  return {
    groups: [
      {
        productId: 'p1',
        productName: 'Foto 10×15',
        sizeId: 's1',
        sizeName: '10×15',
        finishName: 'Lucios',
        items: [
          {
            uploadId: 'u1',
            quantity: 1,
            previewUrl: '/api/uploads/u1/preview',
            unitPrice: SUBTOTAL,
            lineTotal: SUBTOTAL,
            widthPx: 1200,
            heightPx: 1800,
          },
        ],
        totalCopies: 1,
        unitPrice: SUBTOTAL,
        subtotal: SUBTOTAL,
      },
    ],
    subtotal: SUBTOTAL,
    itemCount: 1,
    couponCode: null,
    couponType: null,
    couponStatus: null,
    couponReason: null,
    discountRon: 0,
    totalRon: SUBTOTAL,
    ...overrides,
  };
}

const PERCENT_10 = makeCart({
  couponCode: 'VARA10',
  couponType: 'Percent',
  couponStatus: 'valid',
  discountRon: 25,
  totalRon: 225,
});

describe('CartPage', () => {
  let http: HttpTestingController;

  function setup(seed: CartResponseDto) {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(seed));

    TestBed.configureTestingModule({
      imports: [CartPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated$: of(false),
            isAuthenticated: () => false,
            getAccessToken: () => null,
            getGuestToken: () => 'guest-token',
          },
        },
        { provide: UploadService, useValue: { getPreviewBlob: () => of('blob:preview') } },
      ],
    });

    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CartPage);
    fixture.detectChanges();
    return fixture;
  }

  type Fixture = ReturnType<typeof setup>;

  function text(fixture: Fixture, selector: string): string {
    const el = fixture.debugElement.query(By.css(selector));
    return el ? (el.nativeElement as HTMLElement).textContent!.trim() : '';
  }

  function click(fixture: Fixture, selector: string): void {
    (fixture.debugElement.query(By.css(selector)).nativeElement as HTMLButtonElement).click();
  }

  function applyCode(fixture: Fixture, code: string) {
    fixture.componentInstance.couponCtrl.setValue(code);
    click(fixture, '.coupon-box__apply');
    return http.expectOne(`${BASE}/coupon`);
  }

  afterEach(() => {
    http.verify();
    localStorage.removeItem(CART_STORAGE_KEY);
    TestBed.resetTestingModule();
  });

  it('applies a promo code and shows the discount row and the discounted total', () => {
    const fixture = setup(makeCart());

    const req = applyCode(fixture, 'VARA10');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ code: 'VARA10' });
    req.flush(PERCENT_10);
    fixture.detectChanges();

    expect(text(fixture, '.cart-summary__row--discount')).toContain('25.00');
    expect(text(fixture, '.cart-summary__row--discount')).toContain('VARA10');
    expect(text(fixture, '.cart-summary__row--total')).toContain('225.00');
    expect(text(fixture, '.coupon-box__code')).toContain('VARA10');
  });

  it('shows no discount row while the cart carries no coupon', () => {
    const fixture = setup(makeCart());

    expect(fixture.debugElement.query(By.css('.cart-summary__row--discount'))).toBeNull();
    expect(text(fixture, '.cart-summary__row--total')).toContain('250.00');
  });

  it('renders the Romanian sentence for INVALID_COUPON, chosen by code', () => {
    const fixture = setup(makeCart());

    applyCode(fixture, 'GRESIT').flush(
      { code: 'INVALID_COUPON', detail: 'Coupon not found.' },
      { status: 422, statusText: 'Unprocessable Content' },
    );
    fixture.detectChanges();

    expect(text(fixture, '.coupon-box__error')).toBe('Codul introdus nu este valid sau a expirat.');
  });

  it('renders the Romanian sentence for COUPON_EXHAUSTED, chosen by code', () => {
    const fixture = setup(makeCart());

    applyCode(fixture, 'EPUIZAT').flush(
      { code: 'COUPON_EXHAUSTED', detail: 'Redemption limit reached.' },
      { status: 422, statusText: 'Unprocessable Content' },
    );
    fixture.detectChanges();

    expect(text(fixture, '.coupon-box__error')).toBe('Codul a atins limita de utilizări.');
  });

  it('shows the server threshold sentence for MIN_SUBTOTAL_NOT_MET', () => {
    const fixture = setup(makeCart());

    applyCode(fixture, 'MARE').flush(
      { code: 'MIN_SUBTOTAL_NOT_MET', detail: 'Codul se aplică la comenzi de minimum 300,00 RON.' },
      { status: 422, statusText: 'Unprocessable Content' },
    );
    fixture.detectChanges();

    expect(text(fixture, '.coupon-box__error')).toBe(
      'Codul se aplică la comenzi de minimum 300,00 RON.',
    );
  });

  it('warns about a stale coupon and removes it through the Elimină action', () => {
    const stale = makeCart({
      couponCode: 'VARA10',
      couponType: 'Percent',
      couponStatus: 'stale',
      couponReason: 'COUPON_EXHAUSTED',
      discountRon: 0,
      totalRon: SUBTOTAL,
    });
    const fixture = setup(stale);
    http.expectOne(BASE).flush(stale);
    fixture.detectChanges();

    expect(text(fixture, '.coupon-box__warning')).toBe('Codul a atins limita de utilizări.');
    expect(fixture.debugElement.query(By.css('.cart-summary__row--discount'))).toBeNull();

    click(fixture, '.coupon-box__remove');
    const req = http.expectOne(`${BASE}/coupon`);
    expect(req.request.method).toBe('DELETE');
    req.flush(makeCart());
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.coupon-box__warning'))).toBeNull();
    expect(fixture.debugElement.query(By.css('.coupon-box__input'))).not.toBeNull();
  });

  it('announces free shipping instead of a discount row, which the cart cannot price', () => {
    const freeShipping = makeCart({
      couponCode: 'TRANSPORT0',
      couponType: 'FreeShipping',
      couponStatus: 'valid',
      discountRon: 0,
      totalRon: SUBTOTAL,
    });
    const fixture = setup(freeShipping);
    http.expectOne(BASE).flush(freeShipping);
    fixture.detectChanges();

    expect(text(fixture, '.cart-summary__row--shipping')).toContain(
      'Transport gratuit cu codul TRANSPORT0',
    );
    expect(fixture.debugElement.query(By.css('.cart-summary__row--discount'))).toBeNull();
    expect(text(fixture, '.cart-summary__row--total')).toContain('250.00');
  });

  it('does not call the server for a blank code', () => {
    const fixture = setup(makeCart());

    fixture.componentInstance.couponCtrl.setValue('   ');
    click(fixture, '.coupon-box__apply');

    http.expectNone(`${BASE}/coupon`);
  });

  it('shows the too-many-attempts sentence for a 429, whose body carries no code', () => {
    const fixture = setup(makeCart());

    applyCode(fixture, 'VARA10').flush('Too Many Requests', {
      status: 429,
      statusText: 'Too Many Requests',
    });
    fixture.detectChanges();

    expect(text(fixture, '.coupon-box__error')).toBe(
      'Prea multe încercări. Așteaptă un minut înainte de a încerca din nou.',
    );
  });

  it('sends one request for a double click, so a coupon is never applied twice', () => {
    const fixture = setup(makeCart());

    fixture.componentInstance.couponCtrl.setValue('VARA10');
    click(fixture, '.coupon-box__apply');
    click(fixture, '.coupon-box__apply');

    http.expectOne(`${BASE}/coupon`).flush(PERCENT_10);
    fixture.detectChanges();

    expect(text(fixture, '.cart-summary__row--total')).toContain('225.00');
  });
});
