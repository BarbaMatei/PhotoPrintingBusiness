import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, debounceTime } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ApplyCouponRequest,
  CartRequest,
  CartMergeRequest,
  CartResponseDto,
  EMPTY_CART,
  CART_STORAGE_KEY,
} from '../models/cart.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly base = `${environment.apiUrl}/cart`;

  private readonly cart$$ = new BehaviorSubject<CartResponseDto>(EMPTY_CART);

  /** Full cart state observable. */
  readonly cart$ = this.cart$$.asObservable();

  /** Number of items in the cart. */
  readonly itemCount$ = this.cart$.pipe(map(c => c.itemCount));

  constructor() {
    // On auth state change, load cart from appropriate source
    this.authService.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        this.loadFromServer();
      } else {
        this.loadFromLocalStorage();
      }
    });
  }

  /** Synchronous read of item count. */
  itemCount(): number {
    return this.cart$$.value.itemCount;
  }

  /** Synchronous snapshot of the current cart state. */
  get snapshot(): CartResponseDto {
    return this.cart$$.value;
  }

  /** Loads the current user/guest cart from the server. */
  loadFromServer(): void {
    this.http.get<CartResponseDto>(this.base).subscribe({
      next: cart => this.cart$$.next(cart),
      error: () => {
        // 401 on expired token — clear state silently
        this.cart$$.next(EMPTY_CART);
      },
    });
  }

  /**
   * Replaces all cart items. Returns the updated cart.
   * For guest sessions, the result is also persisted to localStorage.
   */
  setCart(request: CartRequest): Observable<CartResponseDto> {
    return this.http.post<CartResponseDto>(this.base, request).pipe(
      tap(cart => {
        this.cart$$.next(cart);
        if (!this.authService.isAuthenticated()) {
          this.saveToLocalStorage(cart);
        }
      }),
    );
  }

  /**
   * Applies a promo code to the current cart. Returns the recalculated cart.
   * For guest sessions, the result is also persisted to localStorage.
   */
  applyCoupon(code: string): Observable<CartResponseDto> {
    const body: ApplyCouponRequest = { code };
    return this.http.post<CartResponseDto>(`${this.base}/coupon`, body).pipe(
      tap(cart => this.acceptCart(cart)),
    );
  }

  /** Detaches the promo code from the current cart. Returns the recalculated cart. */
  clearCoupon(): Observable<CartResponseDto> {
    return this.http.delete<CartResponseDto>(`${this.base}/coupon`).pipe(
      tap(cart => this.acceptCart(cart)),
    );
  }

  /** Removes all cart items. */
  clearCart(): Observable<void> {
    return this.http.delete<void>(this.base).pipe(
      tap(() => {
        this.cart$$.next(EMPTY_CART);
        localStorage.removeItem(CART_STORAGE_KEY);
      }),
    );
  }

  /**
   * Merges the guest session's cart into the authenticated user's cart.
   * Clears localStorage after a successful merge.
   */
  mergeOnLogin(guestSessionId: string): Observable<CartResponseDto> {
    const body: CartMergeRequest = { guestSessionId };
    return this.http.post<CartResponseDto>(`${this.base}/merge`, body).pipe(
      tap(cart => {
        this.cart$$.next(cart);
        localStorage.removeItem(CART_STORAGE_KEY);
      }),
    );
  }

  // ── Private helpers ──────────────────────────────────────────────────────────

  private acceptCart(cart: CartResponseDto): void {
    this.cart$$.next(cart);
    if (!this.authService.isAuthenticated()) {
      this.saveToLocalStorage(cart);
    }
  }

  private loadFromLocalStorage(): void {
    try {
      const raw = localStorage.getItem(CART_STORAGE_KEY);
      if (raw) {
        const cart = JSON.parse(raw) as CartResponseDto;
        this.cart$$.next(cart);
        if (cart?.couponCode) {
          this.loadFromServer();
        }
      }
    } catch {
      // Corrupted storage — ignore
    }
  }

  private saveToLocalStorage(cart: CartResponseDto): void {
    try {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(cart));
    } catch {
      // Storage quota exceeded — ignore
    }
  }
}

