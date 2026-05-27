import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OrderService } from './order.service';
import { environment } from '../../../environments/environment';

describe('OrderService', () => {
  let service: OrderService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OrderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('getOrders', () => {
    it('calls GET /api/orders with page and pageSize params', () => {
      let result: { items: unknown[]; total: number } | undefined;
      service.getOrders(1, 10).subscribe(r => (result = r));

      const req = http.expectOne(r => r.url.includes('/orders') && !r.url.includes('/orders/'));
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');

      req.flush({ items: [], total: 0, page: 1, size: 10 });
      expect(result).toEqual({ items: [], total: 0 });
    });

    it('maps items and total from response', () => {
      const mockItem = {
        id: 'abc', orderNumber: 'FT-001', status: 'Paid', totalRon: 50,
        createdAt: '2026-01-01', deliveryType: 'Easybox', itemCount: 2,
      };
      let result: { items: unknown[]; total: number } | undefined;
      service.getOrders(2, 5).subscribe(r => (result = r));

      const req = http.expectOne(r => r.url.includes('/orders') && !r.url.includes('/orders/'));
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('5');
      req.flush({ items: [mockItem], total: 1, page: 2, size: 5 });

      expect(result!.items).toHaveLength(1);
      expect(result!.total).toBe(1);
    });
  });

  describe('getOrderDetail', () => {
    it('calls GET /api/orders/:id', () => {
      let result: unknown;
      service.getOrderDetail('order-123').subscribe(r => (result = r));

      const req = http.expectOne(`${environment.apiUrl}/orders/order-123`);
      expect(req.request.method).toBe('GET');
      req.flush({ id: 'order-123', orderNumber: 'FT-001', status: 'Paid' });

      expect((result as { id: string }).id).toBe('order-123');
    });
  });
});
