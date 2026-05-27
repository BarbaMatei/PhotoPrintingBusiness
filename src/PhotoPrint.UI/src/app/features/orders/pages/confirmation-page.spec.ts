import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';
import { ConfirmationPage } from './confirmation-page';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartService } from '../../../core/services/cart.service';
import { OrderDto } from '../../../core/models/payment.model';

function makeOrder(status: string): OrderDto {
  return {
    id: 'order-1',
    orderNumber: 'FT-20260001',
    status: status as OrderDto['status'],
    totalRon: 45.5,
    subtotalRon: 25.5,
    shippingCostRon: 20,
    deliveryType: 'Easybox',
    paymentProcessor: 'Stripe',
    createdAt: '2026-01-01T00:00:00Z',
    paidAt: '2026-01-01T00:01:00Z',
  };
}

describe('ConfirmationPage', () => {
  function setup(overrides: {
    orderStatus?: string;
    orderError?: boolean;
    isAuthenticated?: boolean;
  } = {}) {
    const { orderStatus = 'Paid', orderError = false, isAuthenticated = false } = overrides;

    const mockPayment = {
      getOrder: vi.fn().mockReturnValue(
        orderError ? throwError(() => new Error('Not found')) : of(makeOrder(orderStatus)),
      ),
    };
    const mockAuth = {
      isAuthenticated: vi.fn().mockReturnValue(isAuthenticated),
    };
    const mockState = { reset: vi.fn() };
    const mockCart = { clearCart: vi.fn().mockReturnValue(new Subject()) };

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
        { provide: PaymentService, useValue: mockPayment },
        { provide: AuthService, useValue: mockAuth },
        { provide: CheckoutStateService, useValue: mockState },
        { provide: CartService, useValue: mockCart },
      ],
    });

    return TestBed.createComponent(ConfirmationPage);
  }

  it('shows success content for Paid order', () => {
    const fixture = setup();
    // Provide the orderId input
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();

    const title = fixture.debugElement.query(By.css('.success-title'));
    expect(title).not.toBeNull();
    expect(title.nativeElement.textContent).toContain('confirmată');
  });

  it('shows guest CTA when not authenticated', () => {
    const fixture = setup({ isAuthenticated: false });
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();

    const cta = fixture.debugElement.query(By.css('.guest-cta'));
    expect(cta).not.toBeNull();
  });

  it('shows orders link when authenticated', () => {
    const fixture = setup({ isAuthenticated: true });
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();

    const cta = fixture.debugElement.query(By.css('.auth-cta'));
    expect(cta).not.toBeNull();
  });

  it('shows error state when order fetch fails', () => {
    const fixture = setup({ orderError: true });
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();

    const err = fixture.debugElement.query(By.css('.state-error'));
    expect(err).not.toBeNull();
  });

  it('isAtLeast returns false for Printing when order is Paid', () => {
    const fixture = setup();
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();
    expect(fixture.componentInstance.isAtLeast('Printing')).toBe(false);
  });

  it('isAtLeast returns true for Paid when order is Shipped', () => {
    const fixture = setup({ orderStatus: 'Shipped' });
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();
    expect(fixture.componentInstance.isAtLeast('Paid')).toBe(true);
  });
});
