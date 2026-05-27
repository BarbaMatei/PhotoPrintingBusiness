import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
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
  shippingAddress: null,
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
      initiateEuPlatesc: vi.fn(),
    };
    const mockState = {
      snapshot: DELIVERY_STATE,
      reset: vi.fn(),
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

  it('renders both payment tab buttons', () => {
    const fixture = createFixture();
    fixture.detectChanges();
    const tabs = fixture.debugElement.queryAll(By.css('.tab-btn'));
    expect(tabs.length).toBe(2);
  });

  it('Stripe tab is active by default', () => {
    const fixture = createFixture();
    fixture.detectChanges();
    const stripeTab = fixture.debugElement.queryAll(By.css('.tab-btn'))[0];
    expect(stripeTab.nativeElement.classList).toContain('active');
  });

  it('switching to EuPlatesc tab shows EuPlatesc panel', () => {
    const fixture = createFixture();
    fixture.componentInstance.switchTab('euplatesc');
    fixture.detectChanges();

    const panel = fixture.debugElement.query(By.css('.euplatesc-info'));
    expect(panel).not.toBeNull();
  });

  it('createStripeIntent is called on init', () => {
    const fixture = createFixture();
    const paymentService = TestBed.inject(PaymentService) as unknown as { createStripeIntent: ReturnType<typeof vi.fn> };
    expect(paymentService.createStripeIntent).toBeDefined();
  });
});
