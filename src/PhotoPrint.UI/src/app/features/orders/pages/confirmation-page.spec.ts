import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';
import { ConfirmationPage } from './confirmation-page';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartService } from '../../../core/services/cart.service';
import {
  CheckoutAttemptService,
  CHECKOUT_ATTEMPT_STORAGE_KEY,
} from '../../../core/services/checkout-attempt.service';
import { OrderPaymentStatusDto } from '../../../core/models/payment.model';

function makeOrder(status: string): OrderPaymentStatusDto {
  return {
    id: 'order-1',
    orderNumber: 'FT-20260001',
    status: status as OrderPaymentStatusDto['status'],
    totalRon: 45.5,
    vatRon: 7.27,
    vatRate: 0.19,
    couponCode: null,
    discountRon: 0,
    deliveryType: 'Easybox',
    createdAt: '2026-01-01T00:00:00Z',
    paidAt: status === 'AwaitingPayment' ? null : '2026-01-01T00:01:00Z',
  };
}

describe('ConfirmationPage', () => {
  let getPaymentStatus: ReturnType<typeof vi.fn>;
  let downloadInvoice: ReturnType<typeof vi.fn>;
  let cartSpy: { clearCart: ReturnType<typeof vi.fn> };

  function setup(overrides: {
    orderStatus?: string;
    orderError?: boolean;
    isAuthenticated?: boolean;
    submitted?: boolean;
  } = {}) {
    const {
      orderStatus = 'Paid',
      orderError = false,
      isAuthenticated = false,
      submitted = false,
    } = overrides;

    localStorage.clear();
    if (submitted) {
      localStorage.setItem(
        CHECKOUT_ATTEMPT_STORAGE_KEY,
        JSON.stringify({ key: 'attempt-1', owner: 'anon', createdAt: Date.now(), orderId: 'order-1' }),
      );
    }

    getPaymentStatus = vi.fn().mockReturnValue(
      orderError ? throwError(() => new HttpErrorResponse({ status: 404 })) : of(makeOrder(orderStatus)),
    );
    downloadInvoice = vi.fn().mockReturnValue(of(new Blob(['%PDF-1.4'])));

    const mockAuth = {
      isAuthenticated: vi.fn().mockReturnValue(isAuthenticated),
      currentUserId: vi.fn().mockReturnValue(null),
      getGuestToken: vi.fn().mockReturnValue(null),
    };
    const mockState = { reset: vi.fn() };
    const mockCart = { clearCart: vi.fn().mockReturnValue(new Subject()) };
    cartSpy = mockCart;

    TestBed.configureTestingModule({
      imports: [ConfirmationPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ orderId: 'order-1' }) },
            paramMap: of(convertToParamMap({ orderId: 'order-1' })),
          },
        },
        { provide: PaymentService, useValue: { getPaymentStatus, downloadInvoice } },
        { provide: AuthService, useValue: mockAuth },
        { provide: CheckoutStateService, useValue: mockState },
        { provide: CartService, useValue: mockCart },
      ],
    });

    const fixture = TestBed.createComponent(ConfirmationPage);
    fixture.componentRef.setInput('orderId', 'order-1');
    return fixture;
  }

  afterEach(() => {
    vi.useRealTimers();
    localStorage.clear();
  });

  it('shows success content for Paid order', () => {
    const fixture = setup();
    fixture.detectChanges();

    const title = fixture.debugElement.query(By.css('.success-title'));
    expect(title).not.toBeNull();
    expect(title.nativeElement.textContent).toContain('confirmată');
  });

  it('shows guest CTA when not authenticated', () => {
    const fixture = setup({ isAuthenticated: false });
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.guest-cta'))).not.toBeNull();
  });

  it('shows orders link when authenticated', () => {
    const fixture = setup({ isAuthenticated: true });
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.auth-cta'))).not.toBeNull();
  });

  it('shows error state when the order cannot be read', () => {
    const fixture = setup({ orderError: true });
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.state-error'))).not.toBeNull();
  });

  it('isAtLeast returns false for Printing when order is Paid', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(fixture.componentInstance.isAtLeast('Printing')).toBe(false);
  });

  it('isAtLeast returns true for Paid when order is Shipped', () => {
    const fixture = setup({ orderStatus: 'Shipped' });
    fixture.detectChanges();
    expect(fixture.componentInstance.isAtLeast('Paid')).toBe(true);
  });

  it('reads the guest-readable payment status, not the signed-in-only order detail', () => {
    const fixture = setup();
    fixture.detectChanges();

    expect(getPaymentStatus).toHaveBeenCalledWith('order-1');
  });

  it('keeps the paying customer on the page while the webhook is still in flight', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'AwaitingPayment', submitted: true });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();

    expect(navigate).not.toHaveBeenCalled();
    expect(fixture.debugElement.query(By.css('.settling'))).not.toBeNull();
  });

  it('polls until the webhook marks the order paid', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'AwaitingPayment', submitted: true });
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(0);
    getPaymentStatus.mockReturnValue(of(makeOrder('Paid')));
    await vi.advanceTimersByTimeAsync(3000);
    fixture.detectChanges();

    expect(getPaymentStatus.mock.calls.length).toBe(2);
    expect(fixture.debugElement.query(By.css('.success-title'))).not.toBeNull();
  });

  it('stops polling once the order is paid', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'Paid', submitted: true });
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(30000);

    expect(getPaymentStatus.mock.calls.length).toBe(1);
  });

  it('stops after one rejected read instead of clearing the guest token on every poll', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'AwaitingPayment', submitted: true });
    getPaymentStatus.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(30000);
    fixture.detectChanges();

    expect(getPaymentStatus.mock.calls.length).toBe(1);
    expect(fixture.debugElement.query(By.css('.state-error'))).not.toBeNull();
  });

  // The payment is in flight; a dropped Wi-Fi read says nothing about it, so the panel must stay.
  it('keeps the settling panel when a later poll fails', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'AwaitingPayment', submitted: true });
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.settling'))).not.toBeNull();

    getPaymentStatus.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    await vi.advanceTimersByTimeAsync(3000);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.settling'))).not.toBeNull();
    expect(fixture.debugElement.query(By.css('.state-error'))).toBeNull();
    expect(fixture.debugElement.query(By.css('.settling-warning'))).not.toBeNull();
  });

  // A timer outliving the page clears a basket the customer built after leaving it.
  it('does not clear a cart built after the page was destroyed', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'AwaitingPayment', submitted: true });
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(0);

    getPaymentStatus.mockReturnValue(of(makeOrder('Paid')));
    fixture.destroy();
    await vi.advanceTimersByTimeAsync(10000);

    expect(cartSpy.clearCart).not.toHaveBeenCalled();
  });

  // A detached anchor saves nothing in Firefox, and revoking in the same tick can beat the save.
  it('attaches the invoice link before clicking and revokes the url afterwards', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'Paid' });
    fixture.detectChanges();

    const created = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:fake');
    const revoked = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    let connectedAtClick: boolean | null = null;
    const realClick = HTMLAnchorElement.prototype.click;
    HTMLAnchorElement.prototype.click = function () { connectedAtClick = this.isConnected; };

    try {
      fixture.componentInstance.downloadInvoice();
      expect(connectedAtClick).toBe(true);
      expect(revoked).not.toHaveBeenCalled();
      await vi.advanceTimersByTimeAsync(0);
      expect(revoked).toHaveBeenCalledWith('blob:fake');
    } finally {
      HTMLAnchorElement.prototype.click = realClick;
      created.mockRestore();
      revoked.mockRestore();
    }
  });

  it('shows the order number instead of the homepage when the settle budget runs out', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'AwaitingPayment', submitted: true });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(40000);
    fixture.detectChanges();

    expect(navigate).not.toHaveBeenCalled();
    expect(getPaymentStatus.mock.calls.length).toBeLessThanOrEqual(11);
    const settling = fixture.debugElement.query(By.css('.settling'));
    expect(settling).not.toBeNull();
    expect(settling.nativeElement.textContent).toContain('FT-20260001');
  });

  it('clears the checkout attempt once the order settles', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'Paid', submitted: true });
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();

    expect(TestBed.inject(CheckoutAttemptService).isWaitingFor('order-1')).toBe(false);
    expect(localStorage.getItem(CHECKOUT_ATTEMPT_STORAGE_KEY)).toBeNull();
  });

  it('does not wait for an order this browser never submitted', () => {
    const fixture = setup({ orderStatus: 'AwaitingPayment', submitted: false });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();

    expect(getPaymentStatus.mock.calls.length).toBe(1);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('stops waiting when the payment comes back failed', async () => {
    vi.useFakeTimers();
    const fixture = setup({ orderStatus: 'PaymentFailed', submitted: true });
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(30000);
    fixture.detectChanges();

    expect(getPaymentStatus.mock.calls.length).toBe(1);
    expect(fixture.debugElement.query(By.css('.state-error'))).not.toBeNull();
  });
  it('offers the invoice to a guest, the only route they have to it', () => {
    const fixture = setup({ orderStatus: 'Paid', submitted: true });
    fixture.detectChanges();

    const button = fixture.debugElement.query(By.css('.download-invoice'));
    expect(button).not.toBeNull();
  });

  it('says the invoice is still being prepared when it is not ready yet', () => {
    const fixture = setup({ orderStatus: 'Paid', submitted: true });
    fixture.detectChanges();

    downloadInvoice.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    fixture.componentInstance.downloadInvoice();
    fixture.detectChanges();

    expect(fixture.componentInstance.invoiceMessage()).not.toBeNull();
  });
});