import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { BehaviorSubject } from 'rxjs';
import { ReviewStep } from './review-step';
import { CartService } from '../../../core/services/cart.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartResponseDto } from '../../../core/models/cart.model';
import { DeliveryState } from '../../../core/models/shipping.model';

function makeCart(subtotal: number): CartResponseDto {
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

  it('does not render a street-address line for an Easybox order', () => {
    // The address line is gated on Courier. Seed Easybox WITH a leftover courier address (the real
    // state after switching method) so the gate is what is under test — asserting on a null address
    // would prove nothing, since Angular renders a missing value as blank either way.
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
    expect(summary.textContent).toContain('Box A');           // locker shown
    expect(summary.textContent).not.toContain('Str. Fantoma'); // street line suppressed
    expect(summary.textContent).not.toContain('Timișoara');
  });
});
