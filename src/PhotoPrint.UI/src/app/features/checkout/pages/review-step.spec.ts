import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ReviewStep } from './review-step';
import { CartService } from '../../../core/services/cart.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartResponseDto } from '../../../core/models/cart.model';
import { DeliveryState } from '../../../core/models/shipping.model';

function makeCart(subtotal: number, overrides: Partial<CartResponseDto> = {}): CartResponseDto {
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
            unitPrice: subtotal,
            lineTotal: subtotal,
            widthPx: 1200,
            heightPx: 1800,
          },
        ],
        totalCopies: 1,
        unitPrice: subtotal,
        subtotal,
      },
    ],
    subtotal,
    itemCount: 1,
    couponCode: null,
    couponType: null,
    couponStatus: null,
    couponReason: null,
    discountRon: 0,
    totalRon: subtotal,
    ...overrides,
  };
}

function makeDeliveryState(cost: number): DeliveryState {
  return {
    method: 'Easybox',
    lockerId: 'l1',
    lockerName: 'Box A',
    shippingAddress: null,
    shippingCostRon: cost,
  };
}

describe('ReviewStep', () => {
  let cartSubject: BehaviorSubject<CartResponseDto>;
  let stateSubject: BehaviorSubject<DeliveryState>;

  function createFixture() {
    const fixture = TestBed.createComponent(ReviewStep);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    cartSubject = new BehaviorSubject<CartResponseDto>(makeCart(20));
    stateSubject = new BehaviorSubject<DeliveryState>(makeDeliveryState(20));

    const mockCart = {
      cart$: cartSubject.asObservable(),
      clearCoupon: () => {
        const cleared = makeCart(cartSubject.value.subtotal);
        cartSubject.next(cleared);
        return of(cleared);
      },
    };
    const mockState = {
      snapshot: makeDeliveryState(20),
      deliveryState$: stateSubject.asObservable(),
    };

    TestBed.configureTestingModule({
      imports: [ReviewStep],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: CartService, useValue: mockCart },
        { provide: CheckoutStateService, useValue: mockState },
      ],
    });
  });

  it('grand total = subtotal + shipping cost', () => {
    const fixture = createFixture();
    const comp = fixture.componentInstance;
    // subtotal = 20, shipping = 20, grand = 40
    expect(comp.grandTotal()).toBe(40);
  });

  it('grand total updates when cart changes', () => {
    const fixture = createFixture();
    const comp = fixture.componentInstance;
    cartSubject.next(makeCart(30));
    fixture.detectChanges();
    // subtotal = 30, shipping = 20, grand = 50
    expect(comp.grandTotal()).toBe(50);
  });

  it('Plătește acum button is disabled until terms are checked', () => {
    const fixture = createFixture();
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('Plătește acum button is enabled after terms checked', () => {
    const fixture = createFixture();
    fixture.componentInstance.termsCtrl.setValue(true);
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(false);
  });

  it('shows grand total in template', () => {
    const fixture = createFixture();
    fixture.detectChanges();
    const grandEl = fixture.debugElement.queryAll(By.css('.total-row--grand'));
    expect(grandEl.length).toBeGreaterThan(0);
    expect(grandEl[0].nativeElement.textContent).toContain('40');
  });

  it('renders the fiscal address for an Easybox order, because that address is invoiced', () => {
    // A locker order now collects a fiscal address, and this is the last screen before paying:
    // a mistyped county would otherwise reach the legal invoice unseen.
    stateSubject.next({
      method: 'Easybox',
      lockerId: 'l1',
      lockerName: 'Box A',
      shippingAddress: {
        street: 'Str. Fantoma', number: '99', block: '',
        city: 'Timișoara', county: 'Timiș', postalCode: '300000',
        recipientName: 'Ana Pop', phone: '0712345678',
      },
      shippingCostRon: 20,
    });
    const fixture = createFixture();
    fixture.detectChanges();

    const summary = fixture.debugElement.query(By.css('.delivery-summary')).nativeElement as HTMLElement;
    expect(summary.textContent).toContain('Box A');
    expect(summary.textContent).toContain('Str. Fantoma');
    expect(summary.textContent).toContain('Timișoara');
    expect(summary.textContent).toContain('facturare');
  });

  it('charges subtotal minus discount plus shipping for a percentage coupon', () => {
    cartSubject.next(makeCart(250, {
      couponCode: 'VARA10',
      couponType: 'Percent',
      couponStatus: 'valid',
      discountRon: 25,
      totalRon: 225,
    }));
    const fixture = createFixture();
    fixture.componentInstance.deliveryState.set(makeDeliveryState(19.99));
    fixture.detectChanges();

    expect(fixture.componentInstance.grandTotal()).toBeCloseTo(244.99, 2);
    const discount = fixture.debugElement.query(By.css('.total-row--discount'));
    expect(discount.nativeElement.textContent).toContain('VARA10');
    expect(discount.nativeElement.textContent).toContain('25.00');
  });

  it('drops the shipping cost for a free-shipping coupon, which the order will not charge', () => {
    cartSubject.next(makeCart(250, {
      couponCode: 'TRANSPORT0',
      couponType: 'FreeShipping',
      couponStatus: 'valid',
      discountRon: 0,
      totalRon: 250,
    }));
    const fixture = createFixture();
    fixture.componentInstance.deliveryState.set(makeDeliveryState(19.99));
    fixture.detectChanges();

    expect(fixture.componentInstance.shippingCost()).toBe(0);
    expect(fixture.componentInstance.grandTotal()).toBeCloseTo(250, 2);
    expect(fixture.debugElement.query(By.css('.free-shipping-note'))).not.toBeNull();
  });

  it('keeps charging shipping for a stale free-shipping coupon and offers to remove it', () => {
    cartSubject.next(makeCart(250, {
      couponCode: 'TRANSPORT0',
      couponType: 'FreeShipping',
      couponStatus: 'stale',
      couponReason: 'COUPON_EXHAUSTED',
      discountRon: 0,
      totalRon: 250,
    }));
    const fixture = createFixture();
    fixture.componentInstance.deliveryState.set(makeDeliveryState(19.99));
    fixture.detectChanges();

    expect(fixture.componentInstance.shippingCost()).toBeCloseTo(19.99, 2);
    const warning = fixture.debugElement.query(By.css('.coupon-warning'));
    expect(warning.nativeElement.textContent).toContain('Codul a atins limita de utilizări.');

    (fixture.debugElement.query(By.css('.coupon-warning__remove')).nativeElement as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.coupon-warning'))).toBeNull();
  });

  it('will not send a stale coupon to payment, where the 409 it causes has no way out', () => {
    cartSubject.next(makeCart(250, {
      couponCode: 'TRANSPORT0',
      couponType: 'FreeShipping',
      couponStatus: 'stale',
      couponReason: 'COUPON_EXHAUSTED',
      discountRon: 0,
      totalRon: 250,
    }));
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const fixture = createFixture();
    fixture.componentInstance.termsCtrl.setValue(true);
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);

    fixture.componentInstance.proceed();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('shows a Romanian sentence and stays usable when removing the coupon fails', () => {
    cartSubject.next(makeCart(250, {
      couponCode: 'VARA10',
      couponType: 'Percent',
      couponStatus: 'stale',
      couponReason: 'COUPON_EXHAUSTED',
      discountRon: 0,
      totalRon: 250,
    }));
    TestBed.overrideProvider(CartService, {
      useValue: {
        cart$: cartSubject.asObservable(),
        clearCoupon: () =>
          throwError(() => new HttpErrorResponse({ status: 500 })),
      },
    });
    const fixture = createFixture();
    fixture.detectChanges();

    (fixture.debugElement.query(By.css('.coupon-warning__remove')).nativeElement as HTMLButtonElement).click();
    fixture.detectChanges();

    const error = fixture.debugElement.query(By.css('.coupon-error'));
    expect(error.nativeElement.textContent).toContain('Nu am putut verifica codul acum.');
    expect(fixture.componentInstance.couponPending()).toBe(false);
    expect(fixture.debugElement.query(By.css('.coupon-warning'))).not.toBeNull();
  });
});
