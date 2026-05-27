import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class Header {
  private readonly authService = inject(AuthService);
  private readonly cartService = inject(CartService);

  protected readonly isAuthenticated = toSignal(
    this.authService.isAuthenticated$,
    { initialValue: false },
  );
  protected readonly isAdmin = toSignal(this.authService.isAdmin$, {
    initialValue: false,
  });
  protected readonly cartCount = toSignal(this.cartService.itemCount$, {
    initialValue: 0,
  });

  protected readonly isMobileMenuOpen = signal(false);
  protected readonly isUserMenuOpen = signal(false);

  toggleMobileMenu(): void {
    this.isMobileMenuOpen.update(v => !v);
    if (this.isMobileMenuOpen()) {
      this.isUserMenuOpen.set(false);
    }
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen.update(v => !v);
  }

  logout(): void {
    this.isUserMenuOpen.set(false);
    this.authService.logout();
  }
}
