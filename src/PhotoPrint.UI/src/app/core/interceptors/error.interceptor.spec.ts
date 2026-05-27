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

  it('calls logout on 401', () => {
    const logoutSpy = vi.spyOn(authService, 'logout');
    http.get(`${API_URL}/me`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/me`).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(logoutSpy).toHaveBeenCalled();
  });

  it('navigates to /auth/login on 401', () => {
    http.get(`${API_URL}/me`).subscribe({ error: () => {} });
    httpMock.expectOne(`${API_URL}/me`).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/auth/login');
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
