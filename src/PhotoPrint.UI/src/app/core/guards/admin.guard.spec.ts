import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { adminGuard } from './admin.guard';
import { AuthService } from '../services/auth.service';

const mockRoute = {} as ActivatedRouteSnapshot;
const mockState = {} as RouterStateSnapshot;

describe('adminGuard', () => {
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
    authService = TestBed.inject(AuthService);
  });

  it('returns true when user is an admin', () => {
    (authService as any).isAdmin$$.next(true);
    const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
    expect(result).toBe(true);
  });

  it('returns a UrlTree redirect when user is not an admin', () => {
    (authService as any).isAdmin$$.next(false);
    const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
    expect(result).toBeInstanceOf(UrlTree);
  });

  it('redirects to / when not admin', () => {
    const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState)) as UrlTree;
    expect(result.toString()).toBe('/');
  });
});
