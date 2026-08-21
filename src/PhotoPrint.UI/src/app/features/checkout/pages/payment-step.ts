import {
  Component,
  inject,
  OnInit,
  ChangeDetectionStrategy,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { NgIf } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartService } from '../../../core/services/cart.service';
import { CreateOrderRequest } from '../../../core/models/payment.model';
import { environment } from '../../../../environments/environment';

type PaymentTab = 'stripe' | 'euplatesc';

@Component({
  selector: 'app-payment-step',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgIf],
  template: `
    <div class="payment-step">
      <h2 class="step-title">Plată</h2>

      <!-- Tab switcher -->
      <div class="payment-tabs">
        <button
          class="tab-btn"
          [class.active]="activeTab() === 'stripe'"
          (click)="switchTab('stripe')"
        >
          💳 Card internațional (Stripe)
        </button>
        <button
          class="tab-btn"
          [class.active]="activeTab() === 'euplatesc'"
          (click)="switchTab('euplatesc')"
        >
          🏦 Card românesc (EuPlatesc)
        </button>
      </div>

      <!-- Stripe tab -->
      <div *ngIf="activeTab() === 'stripe'" class="tab-panel">
        <div *ngIf="stripeError()" class="payment-error">{{ stripeError() }}</div>
        <div *ngIf="!stripeReady() && !stripeError()" class="loading-placeholder">
          Se inițializează formularul de plată...
        </div>
        <div id="stripe-card-element" class="stripe-card-element"></div>
        <button
          class="btn btn--primary"
          [disabled]="stripeLoading() || !stripeReady()"
          (click)="payWithStripe()"
        >
          <span *ngIf="stripeLoading()">Se procesează...</span>
          <span *ngIf="!stripeLoading()">Plătește acum</span>
        </button>
      </div>

      <!-- EuPlatesc tab -->
      <div *ngIf="activeTab() === 'euplatesc'" class="tab-panel">
        <p class="euplatesc-info">
          Vei fi redirecționat în mod securizat către pagina EuPlatesc pentru a finaliza plata.
        </p>
        <button
          class="btn btn--primary"
          [disabled]="euPlatescLoading()"
          (click)="payWithEuPlatesc()"
        >
          <span *ngIf="euPlatescLoading()">Se redirecționează...</span>
          <span *ngIf="!euPlatescLoading()">Plătește cu EuPlatesc</span>
        </button>
      </div>

      <div class="step-actions">
        <button type="button" class="btn btn--ghost" (click)="back()">← Înapoi</button>
      </div>
    </div>
  `,
  styles: [`
    .payment-step { display: flex; flex-direction: column; gap: 1.5rem; }
    .step-title { font-size: 1.4rem; font-weight: 600; margin: 0; }

    .payment-tabs {
      display: flex;
      gap: 0;
      border: 1px solid #dee2e6;
      border-radius: 8px;
      overflow: hidden;
    }

    .tab-btn {
      flex: 1;
      padding: 0.75rem;
      background: none;
      border: none;
      cursor: pointer;
      font-size: 0.9rem;
      color: #495057;
      transition: background 0.2s;

      &.active { background: #f0f6ff; color: #1a73e8; font-weight: 600; }
      &:first-child { border-right: 1px solid #dee2e6; }
    }

    .tab-panel { display: flex; flex-direction: column; gap: 1rem; }

    .stripe-card-element {
      padding: 0.75rem;
      border: 1px solid #ced4da;
      border-radius: 6px;
      background: #fff;
      min-height: 42px;
    }

    .loading-placeholder {
      color: #6c757d;
      font-size: 0.9rem;
      text-align: center;
      padding: 1rem;
    }

    .payment-error {
      background: #fff5f5;
      border: 1px solid #fed7d7;
      border-radius: 6px;
      padding: 0.75rem;
      color: #c53030;
      font-size: 0.9rem;
    }

    .euplatesc-info { color: #6c757d; font-size: 0.95rem; line-height: 1.5; }

    .step-actions { padding-top: 0.5rem; }


  `],
})
export class PaymentStep implements OnInit {
  private readonly router = inject(Router);
  private readonly paymentService = inject(PaymentService);
  private readonly checkoutState = inject(CheckoutStateService);
  private readonly cartService = inject(CartService);

  readonly activeTab = signal<PaymentTab>('stripe');
  readonly stripeReady = signal(false);
  readonly stripeLoading = signal(false);
  readonly stripeError = signal<string | null>(null);
  readonly euPlatescLoading = signal(false);

  // Stripe internals (no strong typing to avoid hard dep at compile time)
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private stripeInstance: any = null;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private cardElement: any = null;
  private orderId: string | null = null;
  private clientSecret: string | null = null;

  ngOnInit(): void {
    // Without this a stale session posts a blank address and the 400 surfaces here as "check your cart".
    if (!this.checkoutState.isDeliveryComplete()) {
      this.router.navigate(['/checkout/livrare']);
      return;
    }
    this.initStripe();
  }

  private async initStripe(): Promise<void> {
    try {
      const { loadStripe } = await import('@stripe/stripe-js');
      this.stripeInstance = await loadStripe(environment.stripePublishableKey);
      if (!this.stripeInstance) {
        this.stripeError.set('Stripe nu s-a putut inițializa. Folosiți EuPlatesc.');
        return;
      }

      // Create order + PaymentIntent
      const req = this.buildOrderRequest('Stripe');
      this.paymentService.createStripeIntent(req).subscribe({
        next: resp => {
          this.orderId = resp.orderId;
          this.clientSecret = resp.clientSecret;
          this.mountCardElement();
        },
        error: () => {
          this.stripeError.set('Nu s-a putut crea sesiunea de plată. Verificați că aveți articole în coș.');
        },
      });
    } catch {
      this.stripeError.set('Stripe nu este disponibil momentan. Folosiți EuPlatesc.');
    }
  }

  private mountCardElement(): void {
    if (!this.stripeInstance || !this.clientSecret) return;
    const elements = this.stripeInstance.elements({ clientSecret: this.clientSecret });
    this.cardElement = elements.create('card');
    this.cardElement.mount('#stripe-card-element');
    this.stripeReady.set(true);
  }

  switchTab(tab: PaymentTab): void {
    this.activeTab.set(tab);
  }

  async payWithStripe(): Promise<void> {
    if (!this.stripeInstance || !this.cardElement || !this.clientSecret) return;
    this.stripeLoading.set(true);
    this.stripeError.set(null);

    const result = await this.stripeInstance.confirmCardPayment(this.clientSecret, {
      payment_method: { card: this.cardElement },
    });

    this.stripeLoading.set(false);

    if (result.error) {
      this.stripeError.set(result.error.message ?? 'Plata a eșuat. Verificați datele cardului.');
    } else if (result.paymentIntent?.status === 'succeeded') {
      this.checkoutState.reset();
      this.cartService.clearCart().subscribe();
      this.router.navigate(['/comanda', this.orderId, 'confirmare']);
    }
  }

  payWithEuPlatesc(): void {
    this.euPlatescLoading.set(true);
    const req = this.buildOrderRequest('EuPlatesc');
    this.paymentService.initiateEuPlatesc(req).subscribe({
      next: resp => {
        window.location.href = resp.redirectUrl;
      },
      error: () => {
        this.euPlatescLoading.set(false);
      },
    });
  }

  back(): void {
    this.router.navigate(['/checkout/recapitulare']);
  }

  private buildOrderRequest(processor: 'Stripe' | 'EuPlatesc'): CreateOrderRequest {
    const s = this.checkoutState.snapshot;
    return {
      paymentProcessor: processor,
      deliveryType: s.method ?? 'Courier',
      easyboxLockerId: s.lockerId,
      shippingAddress: s.shippingAddress,
      shippingCostRon: s.shippingCostRon,
    };
  }
}
