import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { PaymentStep } from './payment-step';
import { PaymentService } from '../../../core/services/payment.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartService } from '../../../core/services/cart.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  CheckoutAttemptService,
  CHECKOUT_ATTEMPT_STORAGE_KEY,
} from '../../../core/services/checkout-attempt.service';
import { DeliveryState } from '../../../core/models/shipping.model';
import { StripeIntentResponse } from '../../../core/models/payment.model';

const stripe = vi.hoisted(() => {
  const mount = vi.fn();
  const unmount = vi.fn();
  const create = vi.fn(() => ({ mount, unmount }));
  const confirmCardPayment = vi.fn();
  const instance = { elements: vi.fn(() => ({ create })), confirmCardPayment };
  return { mount, unmount, create, confirmCardPayment, instance, loadStripe: vi.fn() };
});

vi.mock('@stripe/stripe-js', () => ({ loadStripe: stripe.loadStripe }));

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
  let intents: Subject<StripeIntentResponse>[];
  let mockPayment: { createStripeIntent: ReturnType<typeof vi.fn> };

  function createFixture() {
    const fixture = TestBed.createComponent(PaymentStep);
    fixture.detectChanges();
    return fixture;
  }

  async function mountReadyStep() {
    const fixture = createFixture();
    await vi.waitFor(() => expect(mockPayment.createStripeIntent).toHaveBeenCalled());
    intents[intents.length - 1].next({ clientSecret: 'cs_test_1', orderId: 'order-1' });
    fixture.detectChanges();
    return fixture;
  }

  function lastKey(): string {
    const calls = mockPayment.createStripeIntent.mock.calls;
    return calls[calls.length - 1][1] as string;
  }

  beforeEach(() => {
    localStorage.clear();
    intents = [];
    stripe.loadStripe.mockResolvedValue(stripe.instance);
    stripe.confirmCardPayment.mockResolvedValue({ paymentIntent: { status: 'succeeded' } });

    mockPayment = {
      createStripeIntent: vi.fn(() => {
        const subject = new Subject<StripeIntentResponse>();
        intents.push(subject);
        return subject.asObservable();
      }),
    };
    const mockState = {
      snapshot: DELIVERY_STATE,
      reset: vi.fn(),
      isDeliveryComplete: vi.fn().mockReturnValue(true),
    };
    const mockCart = { clearCart: vi.fn().mockReturnValue(new Subject()) };
    const mockAuth = {
      isAuthenticated: () => false,
      currentUserId: () => null,
      getGuestToken: () => 'guest-token-1',
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
        { provide: AuthService, useValue: mockAuth },
      ],
    });
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
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

  it('sends an idempotency key with the intent request', async () => {
    await mountReadyStep();

    expect(mockPayment.createStripeIntent).toHaveBeenCalledTimes(1);
    expect(lastKey()).toMatch(/^[0-9a-f-]{36}$/i);
  });

  it('reuses one idempotency key across two mounts, so one basket cannot become two orders', async () => {
    const first = await mountReadyStep();
    const firstKey = lastKey();
    first.destroy();

    createFixture();
    // mountReadyStep only waits for "called at all", which the first mount already satisfied,
    // so wait for the second call itself.
    await vi.waitFor(() => expect(mockPayment.createStripeIntent).toHaveBeenCalledTimes(2));
    expect(lastKey()).toBe(firstKey);
  });

  it('omits the deprecated shipping cost from the intent request', async () => {
    await mountReadyStep();

    const body = mockPayment.createStripeIntent.mock.calls[0][0] as Record<string, unknown>;
    expect(Object.keys(body).some(k => /shippingcostron/i.test(k))).toBe(false);
  });

  it('sends an incomplete delivery state back to the delivery step instead of posting it', () => {
    const state = TestBed.inject(CheckoutStateService) as unknown as {
      isDeliveryComplete: ReturnType<typeof vi.fn>;
    };
    state.isDeliveryComplete.mockReturnValue(false);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    createFixture();

    expect(navigate).toHaveBeenCalledWith(['/checkout/livrare']);
    expect(mockPayment.createStripeIntent).not.toHaveBeenCalled();
  });

  it('records the created order so the confirmation page waits for its webhook', async () => {
    await mountReadyStep();

    expect(TestBed.inject(CheckoutAttemptService).isWaitingFor('order-1')).toBe(true);
  });

  it('clears the spinner and reports the failure when the card confirmation rejects', async () => {
    const fixture = await mountReadyStep();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    stripe.confirmCardPayment.mockRejectedValue(new Error('network down'));

    await fixture.componentInstance.payWithStripe();

    expect(fixture.componentInstance.stripeLoading()).toBe(false);
    expect(fixture.componentInstance.stripeError()).not.toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('treats a still-processing payment as submitted and sends the customer to the confirmation page', async () => {
    const fixture = await mountReadyStep();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    stripe.confirmCardPayment.mockResolvedValue({ paymentIntent: { status: 'processing' } });

    await fixture.componentInstance.payWithStripe();

    expect(navigate).toHaveBeenCalledWith(['/comanda', 'order-1', 'confirmare']);
    expect(fixture.componentInstance.stripeLoading()).toBe(false);
  });

  it('tells the customer what happened when the result is neither success nor error', async () => {
    const fixture = await mountReadyStep();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    stripe.confirmCardPayment.mockResolvedValue({
      paymentIntent: { status: 'requires_payment_method' },
    });

    await fixture.componentInstance.payWithStripe();

    expect(fixture.componentInstance.stripeError()).not.toBeNull();
    expect(fixture.componentInstance.stripeLoading()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('clears the checkout on a succeeded payment and goes to the confirmation page', async () => {
    const fixture = await mountReadyStep();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const state = TestBed.inject(CheckoutStateService) as unknown as { reset: ReturnType<typeof vi.fn> };
    const cart = TestBed.inject(CartService) as unknown as { clearCart: ReturnType<typeof vi.fn> };

    await fixture.componentInstance.payWithStripe();

    expect(state.reset).toHaveBeenCalled();
    expect(cart.clearCart).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/comanda', 'order-1', 'confirmare']);
  });

  it('starts a fresh attempt once when the basket diverged from the stored key', async () => {
    const fixture = createFixture();
    await vi.waitFor(() => expect(mockPayment.createStripeIntent).toHaveBeenCalled());
    const staleKey = lastKey();

    intents[0].error(
      new HttpErrorResponse({ status: 409, error: { divergentFields: ['items'] } }),
    );
    await vi.waitFor(() => expect(mockPayment.createStripeIntent).toHaveBeenCalledTimes(2));
    fixture.detectChanges();

    expect(lastKey()).not.toBe(staleKey);

    intents[1].error(
      new HttpErrorResponse({ status: 409, error: { divergentFields: ['items'] } }),
    );
    fixture.detectChanges();

    expect(mockPayment.createStripeIntent).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.stripeError()).not.toBeNull();
  });

  it('sends the customer to the order they already paid instead of charging again', async () => {
    const fixture = createFixture();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    await vi.waitFor(() => expect(mockPayment.createStripeIntent).toHaveBeenCalled());

    intents[0].error(new HttpErrorResponse({ status: 409, error: { orderId: 'order-paid-1' } }));
    fixture.detectChanges();

    expect(navigate).toHaveBeenCalledWith(['/comanda', 'order-paid-1', 'confirmare']);
    expect(mockPayment.createStripeIntent).toHaveBeenCalledTimes(1);
    expect(localStorage.getItem(CHECKOUT_ATTEMPT_STORAGE_KEY)).toBeNull();
  });

  it('offers a retry when the intent cannot be created at all', async () => {
    const fixture = createFixture();
    await vi.waitFor(() => expect(mockPayment.createStripeIntent).toHaveBeenCalled());

    intents[0].error(new HttpErrorResponse({ status: 500 }));
    fixture.detectChanges();

    expect(fixture.componentInstance.stripeError()).not.toBeNull();
    expect(fixture.componentInstance.canRetry()).toBe(true);
    expect(fixture.debugElement.query(By.css('.retry-payment'))).not.toBeNull();
  });
});
