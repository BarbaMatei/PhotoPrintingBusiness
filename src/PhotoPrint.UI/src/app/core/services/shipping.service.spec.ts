import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ShippingService } from './shipping.service';
import { environment } from '../../../environments/environment';

describe('ShippingService', () => {
  let service: ShippingService;
  let http: HttpTestingController;
  const BASE = `${environment.apiUrl}/shipping`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ShippingService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getLockers calls correct URL with city param', () => {
    service.getLockers('Cluj').subscribe();
    const req = http.expectOne(`${BASE}/lockers?city=Cluj`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getShippingCost calls correct URL with type param', () => {
    service.getShippingCost('Easybox').subscribe();
    const req = http.expectOne(`${BASE}/cost?type=Easybox`);
    expect(req.request.method).toBe('GET');
    req.flush({ costRon: 20 });
  });
});
