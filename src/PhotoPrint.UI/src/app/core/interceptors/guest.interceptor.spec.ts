import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { guestInterceptor } from './guest.interceptor';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

const API_URL = environment.apiUrl;

describe('guestInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([guestInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    sessionStorage.clear();
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches X-Guest-Token header when guest token is present and user not authenticated', () => {
    vi.spyOn(authService, 'getGuestToken').mockReturnValue('guest-tok-abc');
    (authService as any).isAuthenticated$$.next(false);
    http.get(`${API_URL}/cart`).subscribe();
    const req = httpMock.expectOne(`${API_URL}/cart`);
    expect(req.request.headers.get('X-Guest-Token')).toBe('guest-tok-abc');
    req.flush({});
  });

  it('does not attach guest header when user is authenticated', () => {
    vi.spyOn(authService, 'getGuestToken').mockReturnValue('guest-tok-abc');
    (authService as any).isAuthenticated$$.next(true);
    http.get(`${API_URL}/cart`).subscribe();
    const req = httpMock.expectOne(`${API_URL}/cart`);
    expect(req.request.headers.has('X-Guest-Token')).toBe(false);
    req.flush({});
  });

  it('does not attach guest header when no guest token', () => {
    vi.spyOn(authService, 'getGuestToken').mockReturnValue(null);
    (authService as any).isAuthenticated$$.next(false);
    http.get(`${API_URL}/cart`).subscribe();
    const req = httpMock.expectOne(`${API_URL}/cart`);
    expect(req.request.headers.has('X-Guest-Token')).toBe(false);
    req.flush({});
  });

  it('does not attach guest header for external requests', () => {
    vi.spyOn(authService, 'getGuestToken').mockReturnValue('guest-tok');
    (authService as any).isAuthenticated$$.next(false);
    http.get('https://api.stripe.com/v1/tokens').subscribe();
    const req = httpMock.expectOne('https://api.stripe.com/v1/tokens');
    expect(req.request.headers.has('X-Guest-Token')).toBe(false);
    req.flush({});
  });
});
