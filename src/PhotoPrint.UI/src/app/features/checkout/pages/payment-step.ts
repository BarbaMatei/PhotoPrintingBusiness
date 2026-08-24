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

@Component({
  selector: 'app-payment-step',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgIf],
  template: `
    <div class="payment-step">
      <h2 class="step-title">Plată</h2>

      <div class="payment-panel">
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

      <div class="step-actions">
        <button type="button" class="btn btn--ghost" (click)="back()">← Înapoi</button>
      </div>
    </div>
  `,
  styles: [`
    .payment-step { display: flex; flex-direction: column; gap: 1.5rem; }
    .step-title { font-size: 1.4rem; font-weight: 600; margin: 0; }

    .payment-panel { display: flex; flex-direction: column; gap: 1rem; }

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

    .step-actions { padding-top: 0.5rem; }
  `],
})
export class PaymentStep implements OnInit {
  private readonly router = inject(Router);
  private readonly paymentService = inject(PaymentService);
  private readonly checkoutState = inject(CheckoutStateService);
  private readonly cartService = inject(CartService);

  readonly stripeReady = signal(false);
  readonly stripeLoading = signal(false);
  readonly stripeError = signal<string | null>(null);

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
        this.stripeError.set('Plata nu s-a putut inițializa. Încercați din nou mai târziu.');
        return;
      }

      // Create order + PaymentIntent
      const req = this.buildOrderRequest();
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
      this.stripeError.set('Plata nu este disponibilă momentan. Încercați din nou mai târziu.');
    }
  }

  private mountCardElement(): void {
    if (!this.stripeInstance || !this.clientSecret) return;
    const elements = this.stripeInstance.elements({ clientSecret: this.clientSecret });
    this.cardElement = elements.create('card');
    this.cardElement.mount('#stripe-card-element');
    this.stripeReady.set(true);
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

  back(): void {
    this.router.navigate(['/checkout/recapitulare']);
  }

  private buildOrderRequest(): CreateOrderRequest {
    const s = this.checkoutState.snapshot;
    return {
      deliveryType: s.method ?? 'Courier',
      easyboxLockerId: s.lockerId,
      shippingAddress: s.shippingAddress,
      shippingCostRon: s.shippingCostRon,
    };
  }
}
