import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductService } from './product.service';
import { Product } from '../models/product.model';
import { environment } from '../../../environments/environment';

const MOCK_PRODUCTS: Product[] = [
  {
    id: 'p1',
    name: 'Poze foto',
    productType: 'PhotoPrint',
    imageUrl: null,
    sortOrder: 0,
    sizes: [{ id: 's1', label: '10×15', widthMm: 100, heightMm: 150, pricingTiers: [{ minQuantity: 1, maxQuantity: null, unitPrice: 1.50 }] }],
    finishes: ['Lucioasă', 'Mată'],
  },
];

describe('ProductService', () => {
  let service: ProductService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getCatalog() makes a GET request on first call', () => {
    const results: Product[][] = [];
    service.getCatalog().subscribe(p => results.push(p));

    const req = http.expectOne(`${environment.apiUrl}/products`);
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_PRODUCTS);

    expect(results[0]).toEqual(MOCK_PRODUCTS);
  });

  it('getCatalog() returns cached value without HTTP on second call', () => {
    // First call — primes the cache
    service.getCatalog().subscribe();
    http.expectOne(`${environment.apiUrl}/products`).flush(MOCK_PRODUCTS);

    // Second call — no HTTP request expected
    const results: Product[][] = [];
    service.getCatalog().subscribe(p => results.push(p));
    http.expectNone(`${environment.apiUrl}/products`);
    expect(results[0]).toEqual(MOCK_PRODUCTS);
  });

  it('clearCache() causes next getCatalog() to fetch from API again', () => {
    // Prime cache
    service.getCatalog().subscribe();
    http.expectOne(`${environment.apiUrl}/products`).flush(MOCK_PRODUCTS);

    service.clearCache();

    // Should re-fetch
    service.getCatalog().subscribe();
    const req = http.expectOne(`${environment.apiUrl}/products`);
    req.flush(MOCK_PRODUCTS);
  });

  it('getProduct() makes GET /products/:id', () => {
    let result: Product | undefined;
    service.getProduct('p1').subscribe(p => (result = p));

    const req = http.expectOne(`${environment.apiUrl}/products/p1`);
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_PRODUCTS[0]);

    expect(result).toEqual(MOCK_PRODUCTS[0]);
  });

  it('getProduct() propagates 404 as error', () => {
    let errored = false;
    service.getProduct('unknown').subscribe({ error: () => (errored = true) });

    http.expectOne(`${environment.apiUrl}/products/unknown`).flush(
      { detail: 'Not found' },
      { status: 404, statusText: 'Not Found' },
    );

    expect(errored).toBe(true);
  });
});
