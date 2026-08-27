import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { vi } from 'vitest';
import { PaymentService } from './payment.service';
import { AuthService } from './auth.service';
import { guestInterceptor } from '../interceptors/guest.interceptor';
import { environment } from '../../../environments/environment';
import { CreateOrderRequest } from '../models/payment.model';

const REQUEST: CreateOrderRequest = {
  deliveryType: 'Courier',
  easyboxLockerId: null,
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
};

describe('PaymentService', () => {
  let service: PaymentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([guestInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: vi.fn().mockReturnValue(false),
            getGuestToken: vi.fn().mockReturnValue('guest-token-1'),
          },
        },
      ],
    });
    service = TestBed.inject(PaymentService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the idempotency key as a header the API reads', () => {
    service.createStripeIntent(REQUEST, 'attempt-key-1').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/payments/stripe/intent`);
    expect(req.request.headers.get('Idempotency-Key')).toBe('attempt-key-1');
    req.flush({ clientSecret: 'cs', orderId: 'order-1' });
  });

  it('keeps the guest token alongside the idempotency key', () => {
    service.createStripeIntent(REQUEST, 'attempt-key-1').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/payments/stripe/intent`);
    expect(req.request.headers.get('X-Guest-Token')).toBe('guest-token-1');
    expect(req.request.headers.get('Idempotency-Key')).toBe('attempt-key-1');
    req.flush({ clientSecret: 'cs', orderId: 'order-1' });
  });

  it('never sends the deprecated shipping cost, which the API logs as tampering', () => {
    service.createStripeIntent(REQUEST, 'attempt-key-1').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/payments/stripe/intent`);
    const keys = Object.keys(req.request.body as Record<string, unknown>);
    expect(keys.some(k => /shippingcostron/i.test(k))).toBe(false);
    req.flush({ clientSecret: 'cs', orderId: 'order-1' });
  });

  it('reads the payment status from the guest-readable endpoint', () => {
    service.getPaymentStatus('order-1').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/orders/order-1/payment-status`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
