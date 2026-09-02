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
import { CheckoutAttemptService } from '../../../core/services/checkout-attempt.service';
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
        <button
          *ngIf="canRetry()"
          type="button"
          class="btn btn--ghost retry-payment"
          (click)="retryPayment()"
        >
          Încearcă din nou
        </button>
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
  private readonly attempts = inject(CheckoutAttemptService);

  readonly stripeReady = signal(false);
  readonly stripeLoading = signal(false);
  readonly stripeError = signal<string | null>(null);
  readonly canRetry = signal(false);

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

      this.createIntent(false);
    } catch {
      this.stripeError.set('Plata nu este disponibilă momentan. Încercați din nou mai târziu.');
    }
  }

  // One key per basket, reused across mounts, so a second tab or a Back-then-forward cannot
  // turn one basket into two orders. The server answers 409 two ways and they mean opposites:
  // a divergent payload means the stored key belongs to another basket, a named order means
  // the customer already paid and must be sent there instead of charged again.
  private createIntent(afterDivergence: boolean): void {
    const key = this.attempts.idempotencyKey();
    this.paymentService.createStripeIntent(this.buildOrderRequest(), key).subscribe({
      next: resp => {
        this.orderId = resp.orderId;
        this.clientSecret = resp.clientSecret;
        this.attempts.markOrderCreated(resp.orderId);
        this.mountCardElement();
      },
      error: err => this.handleIntentError(err, afterDivergence),
    });
  }

  private handleIntentError(err: unknown, afterDivergence: boolean): void {
    const response = err as { status?: number; error?: { orderId?: string; divergentFields?: string[] } };
    const settledOrderId = response?.error?.orderId;
    const diverged = !!response?.error?.divergentFields?.length;

    if (response?.status === 409 && settledOrderId) {
      this.attempts.clear();
      this.router.navigate(['/comanda', settledOrderId, 'confirmare']);
      return;
    }

    if (response?.status === 409 && diverged && !afterDivergence) {
      this.attempts.clear();
      this.createIntent(true);
      return;
    }

    this.stripeError.set(
      response?.status === 409
        ? 'Coșul s-a schimbat între timp. Reîncărcați pagina și încercați din nou.'
        : 'Nu s-a putut crea sesiunea de plată. Verificați că aveți articole în coș.',
    );
    this.canRetry.set(true);
  }

  private discardDeadIntent(): void {
    this.clientSecret = null;
    this.stripeReady.set(false);
    this.cardElement?.unmount?.();
    this.cardElement = null;
    this.attempts.retireKey();
    this.canRetry.set(true);
  }

  retryPayment(): void {
    this.canRetry.set(false);
    this.stripeError.set(null);
    this.createIntent(false);
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

    // A rejected confirm call (the network dropped, Stripe.js threw) must not leave the button
    // spinning with nothing said.
    let result: { error?: { message?: string }; paymentIntent?: { status?: string } };
    try {
      result = await this.stripeInstance.confirmCardPayment(this.clientSecret, {
        payment_method: { card: this.cardElement },
      });
    } catch {
      this.stripeLoading.set(false);
      this.stripeError.set('Plata nu a putut fi trimisă. Verificați conexiunea și încercați din nou.');
      return;
    }

    this.stripeLoading.set(false);

    if (result.error) {
      this.stripeError.set(result.error.message ?? 'Plata a eșuat. Verificați datele cardului.');
      // The webhook moves this order to PaymentFailed, and its intent stays chargeable: confirming
      // the same secret again takes money the order can no longer be settled against.
      this.discardDeadIntent();
      return;
    }

    // A payment still in flight is submitted, not failed: the confirmation page waits for the
    // webhook. Any other status means nothing was charged, so say so rather than stranding.
    const status = result.paymentIntent?.status;
    if (status === 'succeeded' || status === 'processing' || status === 'requires_capture') {
      this.attempts.retireKey();
      this.checkoutState.reset();
      this.cartService.clearCart().subscribe();
      this.router.navigate(['/comanda', this.orderId, 'confirmare']);
      return;
    }

    this.stripeError.set(
      'Plata nu a fost finalizată. Verificați datele cardului și încercați din nou.',
    );
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
    };
  }
}
