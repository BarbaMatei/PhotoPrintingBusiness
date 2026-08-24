import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { PaymentStep } from './payment-step';
import { PaymentService } from '../../../core/services/payment.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartService } from '../../../core/services/cart.service';
import { DeliveryState } from '../../../core/models/shipping.model';

const DELIVERY_STATE: DeliveryState = {
  method: 'Easybox',
  lockerId: 'l1',
  lockerName: 'Box A',
  shippingAddress: {
    street: 'Str. Buyer',
    number: '10',
    block: '',
    city: 'Cluj-Napoca',
    county: 'Cluj',
    postalCode: '400100',
    recipientName: 'Ana Pop',
    phone: '0712345678',
  },
  shippingCostRon: 20,
};

describe('PaymentStep', () => {
  let intentSubject: Subject<{ clientSecret: string; orderId: string }>;

  function createFixture() {
    const fixture = TestBed.createComponent(PaymentStep);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    intentSubject = new Subject();

    const mockPayment = {
      createStripeIntent: vi.fn().mockReturnValue(intentSubject.asObservable()),
    };
    const mockState = {
      snapshot: DELIVERY_STATE,
      reset: vi.fn(),
      isDeliveryComplete: vi.fn().mockReturnValue(true),
    };
    const mockCart = {
      clearCart: vi.fn().mockReturnValue(new Subject()),
    };

    TestBed.configureTestingModule({
      imports: [PaymentStep],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: PaymentService, useValue: mockPayment },
        { provide: CheckoutStateService, useValue: mockState },
        { provide: CartService, useValue: mockCart },
      ],
    });
  });

  it('renders the card payment panel directly, with no tab switcher', () => {
    const fixture = createFixture();
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.payment-panel'))).not.toBeNull();
    expect(fixture.debugElement.query(By.css('#stripe-card-element'))).not.toBeNull();
    expect(fixture.debugElement.queryAll(By.css('.tab-btn')).length).toBe(0);
  });

  it('disables the pay button until Stripe is ready', () => {
    const fixture = createFixture();
    fixture.detectChanges();

    const payButton = fixture.debugElement.query(By.css('.payment-panel .btn--primary'));
    expect(payButton.nativeElement.disabled).toBe(true);
  });

  it('createStripeIntent is called on init', () => {
    const fixture = createFixture();
    const paymentService = TestBed.inject(PaymentService) as unknown as { createStripeIntent: ReturnType<typeof vi.fn> };
    expect(paymentService.createStripeIntent).toBeDefined();
  });

  it('sends an incomplete delivery state back to the delivery step instead of posting it', () => {
    const state = TestBed.inject(CheckoutStateService) as unknown as {
      isDeliveryComplete: ReturnType<typeof vi.fn>;
    };
    state.isDeliveryComplete.mockReturnValue(false);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const paymentService = TestBed.inject(PaymentService) as unknown as {
      createStripeIntent: ReturnType<typeof vi.fn>;
    };

    createFixture();

    expect(navigate).toHaveBeenCalledWith(['/checkout/livrare']);
    expect(paymentService.createStripeIntent).not.toHaveBeenCalled();
  });
});
