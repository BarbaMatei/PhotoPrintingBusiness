import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BaseApiService } from './base-api.service';
import { jwtInterceptor } from '../../interceptors/jwt.interceptor';
import { guestInterceptor } from '../../interceptors/guest.interceptor';
import { AuthService } from '../auth.service';
import { environment } from '../../../../environments/environment';

const API = environment.apiUrl;

describe('BaseApiService', () => {
  let api: BaseApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor, guestInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(BaseApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
    localStorage.clear();
  });

  describe('URL building', () => {
    it('prefixes the API root, with or without a leading slash on the path', () => {
      api.get('/orders').subscribe();
      http.expectOne(`${API}/orders`).flush({});

      api.get('orders').subscribe();
      http.expectOne(`${API}/orders`).flush({});
    });

    it('exposes the absolute URL for callers that build their own request', () => {
      expect(api.url('/account/addresses')).toBe(`${API}/account/addresses`);
    });

    it('builds a URL the auth interceptors recognise as this API', () => {
      const auth = TestBed.inject(AuthService);
      vi.spyOn(auth, 'getAccessToken').mockReturnValue('test-jwt');

      api.get('/orders').subscribe();

      const req = http.expectOne(`${API}/orders`);
      expect(req.request.headers.get('Authorization')).toBe('Bearer test-jwt');
      req.flush({});
    });
  });

  describe('query parameters', () => {
    it('sends the parameters it is given, coercing numbers and booleans', () => {
      api.get('/orders', { params: { page: 1, active: true, q: 'cluj' } }).subscribe();

      const req = http.expectOne(`${API}/orders?page=1&active=true&q=cluj`);
      expect(req.request.params.get('page')).toBe('1');
      req.flush({});
    });

    it('drops parameters that are undefined or null', () => {
      api
        .get('/orders', { params: { page: 1, status: undefined, search: null } })
        .subscribe();

      http.expectOne(`${API}/orders?page=1`).flush({});
    });

    it('keeps an empty string, which asks the API for the unfiltered list', () => {
      api.get('/shipping/lockers', { params: { city: '' } }).subscribe();

      http.expectOne(`${API}/shipping/lockers?city=`).flush([]);
    });

    it('sends no query string when every parameter was dropped', () => {
      api.get('/orders', { params: { status: undefined } }).subscribe();

      http.expectOne(`${API}/orders`).flush({});
    });
  });

  describe('verbs', () => {
    it('posts, puts and patches the body it is given', () => {
      api.post('/account/addresses', { city: 'Cluj' }).subscribe();
      const post = http.expectOne(`${API}/account/addresses`);
      expect(post.request.method).toBe('POST');
      expect(post.request.body).toEqual({ city: 'Cluj' });
      post.flush({});

      api.put('/account/addresses/a1', { city: 'Iași' }).subscribe();
      const put = http.expectOne(`${API}/account/addresses/a1`);
      expect(put.request.method).toBe('PUT');
      expect(put.request.body).toEqual({ city: 'Iași' });
      put.flush({});

      api.patch('/account', { firstName: 'Ana' }).subscribe();
      const patch = http.expectOne(`${API}/account`);
      expect(patch.request.method).toBe('PATCH');
      patch.flush({});
    });

    it('deletes', () => {
      api.delete('/account/addresses/a1').subscribe();
      const req = http.expectOne(`${API}/account/addresses/a1`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });

    it('asks for a blob when the endpoint answers with a file', () => {
      api.getBlob('/admin/orders/o1/download-zip').subscribe();
      const req = http.expectOne(`${API}/admin/orders/o1/download-zip`);
      expect(req.request.responseType).toBe('blob');
      req.flush(new Blob(['zip']));
    });

    it('passes custom headers through', () => {
      api.post('/payments', {}, { headers: { 'X-Test': 'value' } }).subscribe();
      const req = http.expectOne(`${API}/payments`);
      expect(req.request.headers.get('X-Test')).toBe('value');
      req.flush({});
    });
  });
});
