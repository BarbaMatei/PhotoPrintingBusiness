import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthService);
    sessionStorage.clear();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('starts unauthenticated', () => {
    expect(service.isAuthenticated()).toBe(false);
  });

  it('starts without admin role', () => {
    expect(service.isAdmin()).toBe(false);
  });

  it('isAuthenticated$ emits false initially', () => {
    const values: boolean[] = [];
    service.isAuthenticated$.subscribe(v => values.push(v));
    expect(values).toEqual([false]);
  });

  it('isAdmin$ emits false initially', () => {
    const values: boolean[] = [];
    service.isAdmin$.subscribe(v => values.push(v));
    expect(values).toEqual([false]);
  });

  it('getAccessToken returns null when sessionStorage is empty', () => {
    expect(service.getAccessToken()).toBeNull();
  });

  it('getAccessToken reads from sessionStorage', () => {
    sessionStorage.setItem('access_token', 'tok123');
    expect(service.getAccessToken()).toBe('tok123');
  });

  it('getGuestToken returns null when localStorage is empty', () => {
    expect(service.getGuestToken()).toBeNull();
  });

  it('getGuestToken reads guestToken from localStorage guestSession JSON', () => {
    localStorage.setItem('guestSession', JSON.stringify({ guestToken: 'guest-abc' }));
    expect(service.getGuestToken()).toBe('guest-abc');
  });

  it('setReturnUrl / getReturnUrl round-trips the URL', () => {
    service.setReturnUrl('/checkout');
    expect(service.getReturnUrl()).toBe('/checkout');
  });

  it('default returnUrl is /tipareste', () => {
    expect(service.getReturnUrl()).toBe('/tipareste');
  });

  it('logout resets isAuthenticated to false', () => {
    (service as any).isAuthenticated$$.next(true);
    service.logout();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('logout resets isAdmin to false', () => {
    (service as any).isAdmin$$.next(true);
    service.logout();
    expect(service.isAdmin()).toBe(false);
  });

  it('logout resets currentUser to null', () => {
    const user = { id: '1', email: 'a@b.com', displayName: 'A', isAdmin: false };
    (service as any).currentUser$$.next(user);
    service.logout();
    const values: unknown[] = [];
    service.currentUser$.subscribe(v => values.push(v));
    expect(values).toEqual([null]);
  });

  it('logout clears the access_token from sessionStorage', () => {
    sessionStorage.setItem('access_token', 'tok');
    service.logout();
    expect(sessionStorage.getItem('access_token')).toBeNull();
  });

  it('logout resets returnUrl to /tipareste', () => {
    service.setReturnUrl('/admin');
    service.logout();
    expect(service.getReturnUrl()).toBe('/tipareste');
  });
});
