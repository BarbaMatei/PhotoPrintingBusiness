import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminService } from './admin.service';
import { environment } from '../../../environments/environment';

describe('AdminService', () => {
  let service: AdminService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/admin`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // ── Stats ────────────────────────────────────────────────────────────────

  describe('getStats', () => {
    it('calls GET /admin/stats/summary', () => {
      let result: unknown;
      service.getStats().subscribe(r => (result = r));

      const req = http.expectOne(`${base}/stats/summary`);
      expect(req.request.method).toBe('GET');
      req.flush({ todayOrders: 3, todayRevenue: 120, monthOrders: 50, monthRevenue: 2000 });

      expect((result as any).todayOrders).toBe(3);
    });
  });

  describe('getRevenueChart', () => {
    it('calls GET /admin/stats/revenue with days param', () => {
      service.getRevenueChart(30).subscribe();

      const req = http.expectOne(r => r.url.includes('/stats/revenue'));
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('days')).toBe('30');
      req.flush([{ date: '2026-05-01', revenue: 150 }]);
    });
  });

  describe('getProductStats', () => {
    it('calls GET /admin/stats/products', () => {
      service.getProductStats().subscribe();

      const req = http.expectOne(`${base}/stats/products`);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });
  });

  describe('getOrdersByStatus', () => {
    it('calls GET /admin/stats/orders-by-status', () => {
      service.getOrdersByStatus().subscribe();

      const req = http.expectOne(`${base}/stats/orders-by-status`);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });
  });

  // ── Orders ────────────────────────────────────────────────────────────────

  describe('getOrders', () => {
    it('calls GET /admin/orders with page and pageSize', () => {
      let result: unknown;
      service.getOrders(1, 20).subscribe(r => (result = r));

      const req = http.expectOne(r => r.url.includes(`${base}/orders`) && !r.url.includes('/orders/'));
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush({ items: [], total: 0 });

      expect((result as any).total).toBe(0);
    });

    it('includes status filter when provided', () => {
      service.getOrders(1, 20, 'Paid').subscribe();

      const req = http.expectOne(r => r.url.includes(`${base}/orders`));
      expect(req.request.params.get('status')).toBe('Paid');
      req.flush({ items: [], total: 0 });
    });

    it('includes search param when provided', () => {
      service.getOrders(1, 20, undefined, 'FT-0001').subscribe();

      const req = http.expectOne(r => r.url.includes(`${base}/orders`));
      expect(req.request.params.get('search')).toBe('FT-0001');
      req.flush({ items: [], total: 0 });
    });
  });

  describe('getOrderDetail', () => {
    it('calls GET /admin/orders/:id', () => {
      let result: unknown;
      service.getOrderDetail('order-abc').subscribe(r => (result = r));

      const req = http.expectOne(`${base}/orders/order-abc`);
      expect(req.request.method).toBe('GET');
      req.flush({ id: 'order-abc', orderNumber: 'FT-001' });

      expect((result as any).id).toBe('order-abc');
    });
  });

  describe('updateOrderStatus', () => {
    it('calls PATCH /admin/orders/:id/status', () => {
      service.updateOrderStatus('order-abc', { status: 'Printing' }).subscribe();

      const req = http.expectOne(`${base}/orders/order-abc/status`);
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual({ status: 'Printing' });
      req.flush({ id: 'order-abc', status: 'Printing' });
    });

    it('includes awbNumber and trackingUrl for Shipped', () => {
      service.updateOrderStatus('order-abc', {
        status: 'Shipped',
        awbNumber: 'AWB123',
        trackingUrl: 'https://track.ro/AWB123',
      }).subscribe();

      const req = http.expectOne(`${base}/orders/order-abc/status`);
      expect(req.request.body.awbNumber).toBe('AWB123');
      req.flush({ id: 'order-abc', status: 'Shipped' });
    });
  });

  describe('cancelOrder', () => {
    it('calls POST /admin/orders/:id/cancel', () => {
      service.cancelOrder('order-abc').subscribe();

      const req = http.expectOne(`${base}/orders/order-abc/cancel`);
      expect(req.request.method).toBe('POST');
      req.flush({ id: 'order-abc', status: 'Cancelled' });
    });
  });

  describe('updateOrderNotes', () => {
    it('calls PATCH /admin/orders/:id/notes', () => {
      service.updateOrderNotes('order-abc', { notes: 'Fragil' }).subscribe();

      const req = http.expectOne(`${base}/orders/order-abc/notes`);
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual({ notes: 'Fragil' });
      req.flush({ id: 'order-abc', internalNotes: 'Fragil' });
    });
  });
});
