import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

const mockRoute = {} as ActivatedRouteSnapshot;

describe('authGuard', () => {
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
    authService = TestBed.inject(AuthService);
  });

  it('returns true when user is authenticated', () => {
    (authService as any).isAuthenticated$$.next(true);
    const mockState = { url: '/comenzile-mele' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
    expect(result).toBe(true);
  });

  it('returns a UrlTree redirect when not authenticated', () => {
    const mockState = { url: '/comenzile-mele' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
    expect(result).toBeInstanceOf(UrlTree);
  });

  it('redirects to /auth/login', () => {
    const mockState = { url: '/contul-meu' } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState)) as UrlTree;
    expect(result.toString()).toBe('/auth/login');
  });

  it('stores the return URL when redirecting', () => {
    const setReturnUrlSpy = vi.spyOn(authService, 'setReturnUrl');
    const mockState = { url: '/contul-meu' } as RouterStateSnapshot;
    TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
    expect(setReturnUrlSpy).toHaveBeenCalledWith('/contul-meu');
  });

  it('guards against open redirect — uses / when state URL is external', () => {
    const setReturnUrlSpy = vi.spyOn(authService, 'setReturnUrl');
    const mockState = { url: 'https://evil.com/steal' } as RouterStateSnapshot;
    TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
    expect(setReturnUrlSpy).toHaveBeenCalledWith('/');
  });
});
