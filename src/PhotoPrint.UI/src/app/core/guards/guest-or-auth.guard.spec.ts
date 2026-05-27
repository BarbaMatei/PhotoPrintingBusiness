import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { guestOrAuthGuard } from './guest-or-auth.guard';
import { AuthService } from '../services/auth.service';

const mockRoute = {} as ActivatedRouteSnapshot;

describe('guestOrAuthGuard', () => {
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
    authService = TestBed.inject(AuthService);
    localStorage.clear();
  });

  it('returns true when user is authenticated', () => {
    (authService as any).isAuthenticated$$.next(true);
    const mockState = { url: '/checkout' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => guestOrAuthGuard(mockRoute, mockState));
    expect(result).toBe(true);
  });

  it('returns true when a guest token is present', () => {
    localStorage.setItem('guestSession', JSON.stringify({ guestToken: 'guest-xyz' }));
    const mockState = { url: '/checkout' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => guestOrAuthGuard(mockRoute, mockState));
    expect(result).toBe(true);
  });

  it('redirects when neither authenticated nor has guest token', () => {
    const mockState = { url: '/checkout' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => guestOrAuthGuard(mockRoute, mockState));
    expect(result).toBeInstanceOf(UrlTree);
  });

  it('redirects to /auth/login when not authenticated', () => {
    const mockState = { url: '/checkout' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => guestOrAuthGuard(mockRoute, mockState)) as UrlTree;
    expect(result.toString()).toBe('/auth/login');
  });

  it('saves the return URL when redirecting', () => {
    const setReturnUrlSpy = vi.spyOn(authService, 'setReturnUrl');
    const mockState = { url: '/checkout' } as RouterStateSnapshot;
    TestBed.runInInjectionContext(() => guestOrAuthGuard(mockRoute, mockState));
    expect(setReturnUrlSpy).toHaveBeenCalledWith('/checkout');
  });
});
