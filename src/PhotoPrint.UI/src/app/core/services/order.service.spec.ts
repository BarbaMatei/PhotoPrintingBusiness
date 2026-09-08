import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OrderService } from './order.service';
import { OrderPhotosDto } from '../models/order.model';
import { environment } from '../../../environments/environment';

describe('OrderService', () => {
  let service: OrderService;
  let http: HttpTestingController;
  const API = 'http://localhost:5052/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OrderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('is written against the dev API root environment.ts pins', () => {
    expect(environment.apiUrl).toBe(API);
  });

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

      const req = http.expectOne(`${API}/orders/order-123`);
      expect(req.request.method).toBe('GET');
      req.flush({ id: 'order-123', orderNumber: 'FT-001', status: 'Paid' });

      expect((result as { id: string }).id).toBe('order-123');
    });
  });

  describe('getOrderPhotos', () => {
    it('calls GET /api/orders/:id/photos and passes the presigned urls through', () => {
      let result: OrderPhotosDto | undefined;
      service.getOrderPhotos('order-123').subscribe(r => (result = r));

      const req = http.expectOne(`${API}/orders/order-123/photos`);
      expect(req.request.method).toBe('GET');
      req.flush({
        photos: [
          {
            uploadId: 'u1',
            fileName: 'poza.jpg',
            thumbnailUrl: 'https://s3/thumb.jpg?sig=abc',
            largeUrl: 'https://s3/large.jpg?sig=abc',
          },
        ],
      });

      expect(result!.photos[0].largeUrl).toBe('https://s3/large.jpg?sig=abc');
    });
  });
});
