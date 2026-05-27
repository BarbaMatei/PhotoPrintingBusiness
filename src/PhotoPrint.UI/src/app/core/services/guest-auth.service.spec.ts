import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { GuestAuthService, GuestSessionData } from './guest-auth.service';
import { environment } from '../../../environments/environment';

const API_URL = environment.apiUrl;

describe('GuestAuthService', () => {
  let service: GuestAuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(GuestAuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('createGuestSession', () => {
    it('sends POST to /api/auth/guest with the dto', () => {
      const dto = { firstName: 'Ion', lastName: 'Pop', email: 'ion@pop.ro', phone: '0712345678' };
      service.createGuestSession(dto).subscribe();
      const req = httpMock.expectOne(`${API_URL}/auth/guest`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(dto);
      req.flush({ guestToken: 'tok-123' });
    });

    it('returns the guestToken from the response', () => {
      const dto = { firstName: 'A', lastName: 'B', email: 'a@b.com', phone: '0712345678' };
      let result: { guestToken: string } | undefined;
      service.createGuestSession(dto).subscribe(r => (result = r));
      httpMock.expectOne(`${API_URL}/auth/guest`).flush({ guestToken: 'my-token' });
      expect(result?.guestToken).toBe('my-token');
    });
  });

  describe('claimGuestSession', () => {
    it('sends POST to /api/auth/guest/claim with the guestToken', () => {
      service.claimGuestSession('tok-abc').subscribe();
      const req = httpMock.expectOne(`${API_URL}/auth/guest/claim`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ guestToken: 'tok-abc' });
      req.flush(null);
    });
  });

  describe('storeSession', () => {
    it('persists session data as JSON to localStorage', () => {
      const data: GuestSessionData = {
        guestToken: 'tok-xyz',
        firstName: 'Ion',
        lastName: 'Pop',
        email: 'ion@pop.ro',
        phone: '0712345678',
      };
      service.storeSession(data);
      const raw = localStorage.getItem('guestSession');
      expect(raw).not.toBeNull();
      expect(JSON.parse(raw!)).toEqual(data);
    });
  });

  describe('getStoredSession', () => {
    it('returns null when localStorage is empty', () => {
      expect(service.getStoredSession()).toBeNull();
    });

    it('returns parsed session data when present', () => {
      const data: GuestSessionData = {
        guestToken: 'tok-xyz',
        firstName: 'Ion',
        lastName: 'Pop',
        email: 'ion@pop.ro',
        phone: '0712345678',
      };
      localStorage.setItem('guestSession', JSON.stringify(data));
      expect(service.getStoredSession()).toEqual(data);
    });

    it('returns null for invalid JSON', () => {
      localStorage.setItem('guestSession', 'not-valid-json{');
      expect(service.getStoredSession()).toBeNull();
    });
  });

  describe('clearSession', () => {
    it('removes guestSession from localStorage', () => {
      localStorage.setItem('guestSession', JSON.stringify({ guestToken: 'tok' }));
      service.clearSession();
      expect(localStorage.getItem('guestSession')).toBeNull();
    });

    it('does not throw when localStorage is already empty', () => {
      expect(() => service.clearSession()).not.toThrow();
    });
  });
});
