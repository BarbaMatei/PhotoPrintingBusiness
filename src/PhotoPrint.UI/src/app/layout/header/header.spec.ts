import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Header } from './header';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';

describe('Header', () => {
  let fixture: ComponentFixture<Header>;
  let component: Header;
  let authService: AuthService;
  let cartService: CartService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Header],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Header);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    cartService = TestBed.inject(CartService);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows auth links when not authenticated', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.header__auth')).not.toBeNull();
    expect(el.querySelector('.header__user')).toBeNull();
  });

  it('hides auth links and shows user area when authenticated', async () => {
    (authService as any).isAuthenticated$$.next(true);
    await fixture.whenStable();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.header__user')).not.toBeNull();
    expect(el.querySelector('.header__auth')).toBeNull();
  });

  it('does not show cart badge when cart is empty', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.header__cart-badge')).toBeNull();
  });

  it('shows cart badge when cart has items', async () => {
    (cartService as any).cart$$.next({
      productId: null, productName: null, finishName: null,
      items: [], subtotal: 0, itemCount: 3,
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.header__cart-badge') as HTMLElement;
    expect(badge).not.toBeNull();
    expect(badge.textContent?.trim()).toBe('3');
  });

  it('does not show admin link when not admin', () => {
    const el = fixture.nativeElement as HTMLElement;
    const adminLink = el.querySelector('.header__nav-link--admin');
    expect(adminLink).toBeNull();
  });

  it('shows admin link when user is admin', async () => {
    (authService as any).isAdmin$$.next(true);
    await fixture.whenStable();
    fixture.detectChanges();
    const adminLink = fixture.nativeElement.querySelector('.header__nav-link--admin');
    expect(adminLink).not.toBeNull();
  });

  it('toggleMobileMenu opens and closes the mobile panel', () => {
    expect(fixture.nativeElement.querySelector('.header__mobile-nav')).toBeNull();
    component.toggleMobileMenu();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.header__mobile-nav')).not.toBeNull();
    component.toggleMobileMenu();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.header__mobile-nav')).toBeNull();
  });

  it('logout calls AuthService.logout()', () => {
    const logoutSpy = vi.spyOn(authService, 'logout');
    component.logout();
    expect(logoutSpy).toHaveBeenCalled();
  });
});
