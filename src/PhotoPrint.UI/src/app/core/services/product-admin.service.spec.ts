import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductAdminService } from './product-admin.service';
import { environment } from '../../../environments/environment';

const API = 'http://localhost:5052/api';
const BASE = `${API}/admin/products`;

describe('ProductAdminService', () => {
  let service: ProductAdminService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductAdminService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('is written against the dev API root the SPA and the e2e stack both use', () => {
    expect(environment.apiUrl).toBe(API);
  });

  it('getAdminProducts asks for GET /admin/products', () => {
    service.getAdminProducts().subscribe();

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('createProduct posts the whole product to /admin/products', () => {
    const request = {
      name: 'Poster',
      productType: 'Poster',
      imageUrl: null,
      sortOrder: 3,
      sizes: [{ label: 'A3', widthMm: 297, heightMm: 420 }],
    };
    service.createProduct(request).subscribe();

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({});
  });

  it('updateProduct puts to /admin/products/:id', () => {
    const request = { name: 'Poster', productType: 'Poster', imageUrl: null, sortOrder: 1 };
    service.updateProduct('p1', request).subscribe();

    const req = http.expectOne(`${BASE}/p1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({});
  });

  it('setProductStatus patches /admin/products/:id/status with the flag', () => {
    service.setProductStatus('p1', false).subscribe();

    const req = http.expectOne(`${BASE}/p1/status`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush({ id: 'p1', isActive: false });
  });

  it('deleteProduct deletes /admin/products/:id', () => {
    service.deleteProduct('p1').subscribe();

    const req = http.expectOne(`${BASE}/p1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('addSize posts to /admin/products/:id/sizes', () => {
    const request = { label: '10x15', widthMm: 100, heightMm: 150 };
    service.addSize('p1', request).subscribe();

    const req = http.expectOne(`${BASE}/p1/sizes`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({});
  });

  it('setSizeStatus patches /admin/products/:id/sizes/:sizeId/status', () => {
    service.setSizeStatus('p1', 's1', true).subscribe();

    const req = http.expectOne(`${BASE}/p1/sizes/s1/status`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isActive: true });
    req.flush({ id: 's1', isActive: true });
  });

  it('replacePricingTiers puts the tiers to /admin/products/:id/sizes/:sizeId/pricing', () => {
    const request = { tiers: [{ minQuantity: 1, maxQuantity: 9, unitPrice: 2.5 }] };
    service.replacePricingTiers('p1', 's1', request).subscribe();

    const req = http.expectOne(`${BASE}/p1/sizes/s1/pricing`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({});
  });

  it('replaceFinishes puts the names to /admin/products/:id/finishes', () => {
    service.replaceFinishes('p1', ['Lucios', 'Mat']).subscribe();

    const req = http.expectOne(`${BASE}/p1/finishes`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ names: ['Lucios', 'Mat'] });
    req.flush(null);
  });
});
