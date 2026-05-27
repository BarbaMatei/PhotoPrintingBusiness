import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { jwtInterceptor } from './jwt.interceptor';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

const API_URL = environment.apiUrl;

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    sessionStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches Authorization header for API requests when token is present', () => {
    vi.spyOn(authService, 'getAccessToken').mockReturnValue('test-jwt');
    http.get(`${API_URL}/products`).subscribe();
    const req = httpMock.expectOne(`${API_URL}/products`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-jwt');
    req.flush({});
  });

  it('does not attach header for API requests when no token', () => {
    vi.spyOn(authService, 'getAccessToken').mockReturnValue(null);
    http.get(`${API_URL}/products`).subscribe();
    const req = httpMock.expectOne(`${API_URL}/products`);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('does not attach header for external requests even when token exists', () => {
    vi.spyOn(authService, 'getAccessToken').mockReturnValue('my-token');
    http.get('https://js.stripe.com/v3/').subscribe();
    const req = httpMock.expectOne('https://js.stripe.com/v3/');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
