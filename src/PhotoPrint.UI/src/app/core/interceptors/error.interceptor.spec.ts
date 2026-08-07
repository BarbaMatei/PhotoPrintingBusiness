import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../../shared/services/toast.service';

const API_URL = 'https://localhost:5001/api';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let toastService: ToastService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    toastService = TestBed.inject(ToastService);
    router = TestBed.inject(Router);
    // Prevent actual navigation so the empty route config does not throw NG04002
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('logs out an authenticated user on 401', () => {
    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(true);
    const logoutSpy = vi.spyOn(authService, 'logout');
    http.get(`${API_URL}/me`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/me`).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(logoutSpy).toHaveBeenCalled();
  });

  it('navigates an authenticated user to /auth/login on 401', () => {
    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(true);
    http.get(`${API_URL}/me`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/me`).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/auth/login');
  });

  // The guest-401 self-heal branch — the core of the bolt's auth
  // change — previously had zero coverage; both existing tests only hit the logout branch.
  it('clears the guest token (no logout/navigation) on 401 for a guest', () => {
    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(false);
    vi.spyOn(authService, 'getGuestToken').mockReturnValue('guest-token');
    const clearSpy = vi.spyOn(authService, 'clearGuestToken');
    const logoutSpy = vi.spyOn(authService, 'logout');

    http.get(`${API_URL}/uploads/x/preview`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/uploads/x/preview`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(clearSpy).toHaveBeenCalled();
    expect(logoutSpy).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  // An anonymous client with no/corrupt guest token must NOT be
  // bounced to a login page it has no account for — treat any unauthenticated 401 as a
  // stale/absent guest session.
  it('does not navigate an anonymous user (no guest token) to login on 401', () => {
    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(false);
    vi.spyOn(authService, 'getGuestToken').mockReturnValue(null);
    const logoutSpy = vi.spyOn(authService, 'logout');

    http.get(`${API_URL}/uploads/x/preview`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/uploads/x/preview`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logoutSpy).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  // C3: the clear->re-init->retry self-heal seam was only unit-tested with each
  // half mocked (the interceptor spies clearGuestToken; the component nulls the token by hand).
  // If the interceptor's clear and the component's getGuestToken diverged on storage key/shape,
  // both isolated tests still pass. Exercise the REAL clear against the REAL reader end-to-end.
  it('actually clears the stored guest token that getGuestToken reads on a guest 401 (real seam — C3)', () => {
    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(false);
    localStorage.setItem('guestSession', JSON.stringify({ guestToken: 'seeded' }));
    expect(authService.getGuestToken()).toBe('seeded');   // precondition: reader sees it

    http.get(`${API_URL}/uploads/x/preview`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/uploads/x/preview`).flush(null, { status: 401, statusText: 'Unauthorized' });

    // The interceptor's real clearGuestToken must remove the SAME key getGuestToken reads.
    expect(authService.getGuestToken()).toBeNull();
  });

  it('shows error toast on 403', () => {
    const showSpy = vi.spyOn(toastService, 'show');
    http.get(`${API_URL}/admin`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/admin`).flush(null, { status: 403, statusText: 'Forbidden' });
    expect(showSpy).toHaveBeenCalledWith(expect.stringContaining('Acces interzis'), 'error');
  });

  it('shows error toast on 500', () => {
    const showSpy = vi.spyOn(toastService, 'show');
    http.get(`${API_URL}/data`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/data`).flush(null, { status: 500, statusText: 'Server Error' });
    expect(showSpy).toHaveBeenCalledWith(expect.stringContaining('Eroare de server'), 'error');
  });

  it('shows warning toast on status 0 (network error)', () => {
    const showSpy = vi.spyOn(toastService, 'show');
    http.get(`${API_URL}/data`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/data`).flush(null, { status: 0, statusText: 'Unknown Error' });
    expect(showSpy).toHaveBeenCalledWith(expect.stringContaining('Eroare de rețea'), 'warning');
  });

  it('re-throws the error so the subscriber can handle it', () => {
    let caught = false;
    http.get(`${API_URL}/data`).subscribe({ error: () => { caught = true; } });
    httpMock.expectOne(`${API_URL}/data`).flush(null, { status: 500, statusText: 'Server Error' });
    expect(caught).toBe(true);
  });

  it('does not show toast for successful responses', () => {
    const showSpy = vi.spyOn(toastService, 'show');
    http.get(`${API_URL}/data`).subscribe();
    httpMock.expectOne(`${API_URL}/data`).flush({ ok: true });
    expect(showSpy).not.toHaveBeenCalled();
  });
});
