import {
  Component,
  inject,
  OnInit,
  ChangeDetectionStrategy,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CartService } from '../../../core/services/cart.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import {
  CartResponseDto,
  cartDiscount,
  cartTotal,
  hasFreeShippingCoupon,
  isCouponStale,
} from '../../../core/models/cart.model';
import { couponErrorMessage, couponMessageFor } from '../../../core/models/coupon-messages';

@Component({
  selector: 'app-review-step',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, RouterLink, ReactiveFormsModule],
  template: `
    <div class="review-step">
      <h2 class="step-title">Recapitulare comandă</h2>

      <!-- Cart items grouped by product+size -->
      <div class="cart-items">
        @for (group of cart()?.groups ?? []; track group.sizeId) {
          <div class="group-header">📷 {{ group.productName }} – {{ group.sizeName }}{{ group.finishName ? ' (' + group.finishName + ')' : '' }}</div>
          @for (item of group.items; track item.uploadId) {
            <div class="item-info">
              <div class="item-qty">{{ item.quantity }} × {{ group.unitPrice | number:'1.2-2' }} RON = {{ item.lineTotal | number:'1.2-2' }} RON</div>
            </div>
          }
        }
      </div>

      <!-- Delivery info -->
      <div class="delivery-summary">
        <div class="summary-row">
          <span>Livrare:</span>
          <span>
            {{ deliveryState().method === 'Easybox' ? '📦 Easybox Sameday' : '🚚 Curier la domiciliu' }}
            @if (deliveryState().lockerName) { <span> — {{ deliveryState().lockerName }}</span> }
            @if (deliveryState().shippingAddress) {
              <span class="fiscal-address">
                — {{ deliveryState().method === 'Easybox' ? 'facturare: ' : '' }}{{ deliveryState().shippingAddress!.street }}
                {{ deliveryState().shippingAddress!.number }}, {{ deliveryState().shippingAddress!.city }},
                {{ deliveryState().shippingAddress!.county }} {{ deliveryState().shippingAddress!.postalCode }}
              </span>
            }
          </span>
        </div>
        <div class="summary-row">
          <span>Estimat livrare:</span>
          <span>2–4 zile lucrătoare</span>
        </div>
      </div>

      @if (couponStale()) {
        <div class="coupon-warning">
          <span>{{ staleMessage() }}</span>
          <button
            type="button"
            class="coupon-warning__remove"
            [disabled]="couponPending()"
            (click)="removeCoupon()"
          >Elimină codul</button>
        </div>
      }
      @if (couponError()) {
        <p class="coupon-error">{{ couponError() }}</p>
      }

      <!-- Totals -->
      <div class="totals">
        <div class="total-row">
          <span>Subtotal:</span>
          <span>{{ cart()?.subtotal | number:'1.2-2' }} RON</span>
        </div>
        @if (discountRon() > 0) {
          <div class="total-row total-row--discount">
            <span>Reducere{{ cart()?.couponCode ? ' (' + cart()!.couponCode + ')' : '' }}:</span>
            <span>-{{ discountRon() | number:'1.2-2' }} RON</span>
          </div>
        }
        <div class="total-row">
          <span>Transport:</span>
          <span>
            {{ shippingCost() | number:'1.2-2' }} RON
            @if (freeShipping()) {
              <span class="free-shipping-note">transport gratuit cu codul {{ cart()!.couponCode }}</span>
            }
          </span>
        </div>
        <div class="total-row total-row--grand">
          <span>Total:</span>
          <span>{{ grandTotal() | number:'1.2-2' }} RON</span>
        </div>
      </div>

      <!-- Terms -->
      <label class="terms-row">
        <input type="checkbox" [formControl]="termsCtrl" />
        <span>
          Sunt de acord cu
          <a routerLink="/termeni-si-conditii" target="_blank">termenii și condițiile</a>
        </span>
      </label>

      <div class="step-actions">
        <button type="button" class="btn btn--ghost" (click)="back()">← Înapoi</button>
        <button
          type="button"
          class="btn btn--primary"
          [disabled]="!termsCtrl.value"
          (click)="proceed()"
        >
          Plătește acum →
        </button>
      </div>
    </div>
  `,
  styles: [`
    .review-step { display: flex; flex-direction: column; gap: 1.5rem; }
    .step-title { font-size: 1.4rem; font-weight: 600; margin: 0; }

    .cart-items { display: flex; flex-direction: column; gap: 0.75rem; }

    .cart-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem;
      border: 1px solid #dee2e6;
      border-radius: 8px;
    }

    .item-thumb {
      width: 64px;
      height: 64px;
      object-fit: cover;
      border-radius: 4px;
      background: #f8f9fa;
    }

    .item-info { flex: 1; }
    .item-format { font-weight: 500; }
    .item-qty { font-size: 0.85rem; color: #6c757d; }
    .item-total { font-weight: 700; white-space: nowrap; }

    .delivery-summary {
      background: #f8f9fa;
      border-radius: 8px;
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .summary-row {
      display: flex;
      gap: 0.5rem;
      font-size: 0.95rem;
      span:first-child { color: #6c757d; min-width: 100px; }
    }

    .totals {
      border-top: 1px solid #dee2e6;
      padding-top: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .total-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.95rem;
      color: #495057;

      &--grand {
        font-size: 1.2rem;
        font-weight: 700;
        color: #212529;
        margin-top: 0.5rem;
      }
    }

    .total-row--discount { color: #188038; font-weight: 600; }

    .free-shipping-note { font-size: 0.8rem; color: #188038; }

    .coupon-warning {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 0.75rem;
      background: #fef7e0;
      border: 1px solid #f9d67a;
      border-radius: 8px;
      padding: 0.75rem 1rem;
      font-size: 0.9rem;
      color: #b06000;
    }

    .coupon-warning__remove {
      background: none;
      border: none;
      cursor: pointer;
      color: #b06000;
      font-weight: 600;
      font-size: 0.85rem;
      text-decoration: underline;

      &:disabled { color: #9aa0a6; cursor: default; text-decoration: none; }
    }

    .coupon-error { margin: 0; font-size: 0.9rem; color: #d93025; }

    .terms-row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.95rem;
      cursor: pointer;

      a { color: #1a73e8; }
    }

    .step-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 0.5rem;
    }


  `],
})
export class ReviewStep implements OnInit {
  private readonly router = inject(Router);
  private readonly cartService = inject(CartService);
  readonly checkoutState = inject(CheckoutStateService);
  private readonly fb = inject(FormBuilder);

  readonly cart = signal<CartResponseDto | null>(null);
  readonly deliveryState = signal(this.checkoutState.snapshot);

  readonly termsCtrl = this.fb.control(false, Validators.requiredTrue);

  readonly couponPending = signal(false);
  readonly couponError = signal<string | null>(null);

  readonly discountRon = () => cartDiscount(this.cart());
  readonly freeShipping = () => hasFreeShippingCoupon(this.cart());
  readonly couponStale = () => isCouponStale(this.cart());
  readonly staleMessage = () => couponMessageFor(this.cart()?.couponReason);

  readonly shippingCost = () =>
    this.freeShipping() ? 0 : this.deliveryState().shippingCostRon;

  readonly grandTotal = () => cartTotal(this.cart()) + this.shippingCost();

  removeCoupon(): void {
    if (this.couponPending()) return;

    this.couponPending.set(true);
    this.couponError.set(null);
    this.cartService.clearCoupon().subscribe({
      next: () => this.couponPending.set(false),
      error: (err: HttpErrorResponse) => {
        this.couponPending.set(false);
        this.couponError.set(couponErrorMessage(err));
      },
    });
  }

  ngOnInit(): void {
    this.cartService.cart$.subscribe(c => this.cart.set(c));
    this.checkoutState.deliveryState$.subscribe(s => this.deliveryState.set(s));
  }

  back(): void {
    this.router.navigate(['/checkout/livrare']);
  }

  proceed(): void {
    if (!this.termsCtrl.value) return;
    this.router.navigate(['/checkout/plata']);
  }
}
