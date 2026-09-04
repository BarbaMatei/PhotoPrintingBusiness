import {
  Component,
  DestroyRef,
  inject,
  OnInit,
  ChangeDetectionStrategy,
  signal,
  input,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe, PercentPipe } from '@angular/common';
import { isAtLeast as isAtLeastFn } from '../../../core/models/order-status.constants';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CheckoutAttemptService } from '../../../core/services/checkout-attempt.service';
import { CartService } from '../../../core/services/cart.service';
import { OrderPaymentStatusDto } from '../../../core/models/payment.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';

const SETTLED_STATUSES = ['Paid', 'Printing', 'Shipped', 'Delivered'];

// Three seconds between reads, ten reads: half a minute covers a normal webhook, and the page
// then keeps the order number on screen instead of pretending the payment failed.
const SETTLE_POLL_MS = 3000;
const MAX_SETTLE_POLLS = 10;

@Component({
  selector: 'app-confirmation',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, PercentPipe, RouterLink, SpinnerComponent],
  template: `
    <div class="confirmation-page">
      @if (loading()) {
        <app-spinner label="Se verifică comanda..." [showLabel]="true" />
      }

      @if (!loading() && order()) {
        <div class="confirmation-content">
        <!-- Success animation -->
        <div class="success-icon">✓</div>
        <h1 class="success-title">Comandă confirmată!</h1>
        <p class="order-number">Comanda <strong>#{{ order()!.orderNumber }}</strong></p>

        <!-- Order summary -->
        <div class="order-summary">
          @if (order()!.discountRon > 0) {
            <div class="summary-row summary-row--discount">
              <span>Reducere{{ order()!.couponCode ? ' (' + order()!.couponCode + ')' : '' }}:</span>
              <span>-{{ order()!.discountRon | number:'1.2-2' }} RON</span>
            </div>
          }
          <div class="summary-row">
            <span>Total plătit:</span>
            <strong>{{ order()!.totalRon | number:'1.2-2' }} RON</strong>
          </div>
          <div class="summary-row">
            <span>din care TVA ({{ order()!.vatRate | percent:'1.0-2' }}):</span>
            <span>{{ order()!.vatRon | number:'1.2-2' }} RON</span>
          </div>
          <div class="summary-row">
            <span>Livrare:</span>
            <span>{{ order()!.deliveryType === 'Easybox' ? 'Easybox Sameday' : 'Curier la domiciliu' }}</span>
          </div>
          <div class="summary-row">
            <span>Estimat:</span>
            <span>2–4 zile lucrătoare</span>
          </div>
        </div>

        <!-- Order status stepper -->
        <div class="status-stepper">
          <div class="status-step done">
            <div class="status-icon">✓</div>
            <div class="status-label">Comandă primită</div>
          </div>
          <div class="status-divider"></div>
          <div class="status-step" [class.done]="isAtLeast('Printing')">
            <div class="status-icon">🖨</div>
            <div class="status-label">În pregătire</div>
          </div>
          <div class="status-divider"></div>
          <div class="status-step" [class.done]="isAtLeast('Shipped')">
            <div class="status-icon">📦</div>
            <div class="status-label">Expediată</div>
          </div>
          <div class="status-divider"></div>
          <div class="status-step" [class.done]="isAtLeast('Delivered')">
            <div class="status-icon">🏠</div>
            <div class="status-label">Livrată</div>
          </div>
        </div>

        <div class="invoice-actions">
          <button
            type="button"
            class="btn btn--ghost download-invoice"
            [disabled]="invoiceLoading()"
            (click)="downloadInvoice()"
          >
            {{ invoiceLoading() ? 'Se descarcă...' : 'Descarcă factura' }}
          </button>
          @if (invoiceMessage()) {
            <p class="invoice-message">{{ invoiceMessage() }}</p>
          }
        </div>

        <!-- CTA based on auth state -->
        @if (!isAuthenticated()) {
          <div class="guest-cta">
            <p>Vrei să urmărești comanda și să o salvezi?</p>
            <a routerLink="/auth/register" class="btn btn--primary">Creează cont gratuit</a>
          </div>
        }
        @if (isAuthenticated()) {
          <div class="auth-cta">
            <a routerLink="/comenzile-mele" class="btn btn--primary">Vezi istoricul comenzilor</a>
          </div>
        }
        </div>
      }

      @if (!loading() && settling()) {
        <div class="settling">
          <app-spinner label="Se confirmă plata..." [showLabel]="true" />
          <p>
            Plata a fost trimisă pentru comanda <strong>#{{ settling()!.orderNumber }}</strong>.
            Confirmarea de la procesatorul de plăți poate întârzia câteva momente.
          </p>
          <p class="settling-hint">
            Puteți închide pagina — comanda este înregistrată și veți primi un e-mail.
          </p>
          @if (pollFailed()) {
            <p class="settling-warning">
              Nu am putut verifica starea acum. Comanda este trimisă; reîncărcați pagina în câteva momente.
            </p>
          }
          @if (settleGaveUp()) {
            <p class="settling-warning">
              Confirmarea întârzie mai mult decât de obicei. Comanda este înregistrată — reîncărcați
              pagina sau verificați e-mailul.
            </p>
          }
        </div>
      }

      @if (!loading() && !order() && !settling()) {
        <div class="state-error">
          <p>Comanda nu a fost găsită sau nu a fost finalizată.</p>
          <a routerLink="/" class="btn btn--primary">Înapoi acasă</a>
        </div>
      }
    </div>
  `,
  styles: [`
    .confirmation-page {
      max-width: 600px;
      margin: 3rem auto;
      padding: 1.5rem 1rem;
      text-align: center;
    }

    // .state-loading removed — replaced by <app-spinner>

    .confirmation-content { display: flex; flex-direction: column; align-items: center; gap: 1.5rem; }

    .success-icon {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: #22c55e;
      color: #fff;
      font-size: 2.5rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .success-title { font-size: 1.8rem; font-weight: 700; margin: 0; }
    .order-number { font-size: 1rem; color: #6c757d; margin: 0; }

    .order-summary {
      width: 100%;
      background: #f8f9fa;
      border-radius: 8px;
      padding: 1rem 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      text-align: left;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.95rem;
      span:first-child { color: #6c757d; }
    }

    .summary-row--discount {
      color: #188038;
      font-weight: 600;
      span:first-child { color: #188038; }
    }

    .status-stepper {
      display: flex;
      align-items: center;
      width: 100%;
      gap: 0;
    }

    .status-step {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.3rem;
      opacity: 0.4;
      transition: opacity 0.3s;

      &.done { opacity: 1; }
    }

    .status-icon {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #e9ecef;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.1rem;
    }

    .status-step.done .status-icon { background: #d1fae5; }

    .status-label { font-size: 0.75rem; text-align: center; max-width: 70px; }

    .status-divider {
      flex: 1;
      height: 2px;
      background: #dee2e6;
    }

    .guest-cta, .auth-cta { display: flex; flex-direction: column; align-items: center; gap: 0.75rem; }
    .guest-cta p { margin: 0; color: #495057; }

    .state-error { display: flex; flex-direction: column; align-items: center; gap: 1rem; }

  `],
})
export class ConfirmationPage implements OnInit {
  readonly orderId = input.required<string>();

  private readonly router = inject(Router);
  private readonly paymentService = inject(PaymentService);
  private readonly authService = inject(AuthService);
  private readonly checkoutState = inject(CheckoutStateService);
  private readonly cartService = inject(CartService);
  private readonly attempts = inject(CheckoutAttemptService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly order = signal<OrderPaymentStatusDto | null>(null);
  readonly settling = signal<OrderPaymentStatusDto | null>(null);
  readonly invoiceMessage = signal<string | null>(null);
  readonly invoiceLoading = signal(false);
  readonly pollFailed = signal(false);
  readonly settleGaveUp = signal(false);

  private polls = 0;
  private settleTimer: ReturnType<typeof setTimeout> | null = null;

  readonly isAuthenticated = () => this.authService.isAuthenticated();

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => {
      if (this.settleTimer !== null) clearTimeout(this.settleTimer);
      this.settleTimer = null;
    });
    this.read();
  }

  // The card confirmation returns before the payment webhook has marked the order paid, so a
  // customer who just paid arrives here on an order still awaiting payment. Sending them home
  // for that loses the confirmation and their emptied basket, so wait instead — but only for an
  // order this browser submitted, and only until the budget runs out.
  private read(): void {
    this.paymentService.getPaymentStatus(this.orderId()).subscribe({
      next: order => {
        this.loading.set(false);

        if (SETTLED_STATUSES.includes(order.status)) {
          const wasWaiting = this.attempts.isWaitingFor(this.orderId());
          this.settling.set(null);
          this.order.set(order);
          if (wasWaiting) {
            this.attempts.clear();
            this.checkoutState.reset();
            this.cartService.clearCart().subscribe();
          }
          return;
        }

        const stillPaying = order.status === 'AwaitingPayment';
        if (!stillPaying || !this.attempts.isWaitingFor(this.orderId())) {
          // Either the payment came back failed, or this browser never submitted this order:
          // nothing to wait for, so show the not-finalised state rather than polling.
          this.settling.set(null);
          return;
        }

        this.settling.set(order);
        this.pollFailed.set(false);
        if (this.polls < MAX_SETTLE_POLLS) {
          this.polls++;
          // A timer that outlives the page would clear a basket built after it.
          this.settleTimer = setTimeout(() => this.read(), SETTLE_POLL_MS);
          return;
        }
        this.settleGaveUp.set(true);
      },
      // A later read failing says nothing about the payment, which is still in flight; only the
      // very first read may fall through to the not-found state.
      error: () => {
        this.loading.set(false);
        if (this.settling()) {
          this.pollFailed.set(true);
          return;
        }
        this.settling.set(null);
      },
    });
  }

  // A guest has no order list, so this page is their only route to a legally required document.
  downloadInvoice(): void {
    this.invoiceLoading.set(true);
    this.invoiceMessage.set(null);

    this.paymentService.downloadInvoice(this.orderId()).subscribe({
      next: blob => {
        this.invoiceLoading.set(false);
        this.saveBlob(blob);
      },
      error: (err: { status?: number }) => {
        this.invoiceLoading.set(false);
        this.invoiceMessage.set(
          err?.status === 404
            ? 'Factura se pregătește. Încercați din nou în câteva minute.'
            : 'Factura nu a putut fi descărcată. Încercați din nou mai târziu.',
        );
      },
    });
  }

  private saveBlob(blob: Blob): void {
    const order = this.order();
    const name = order ? `factura-${order.orderNumber}.pdf` : 'factura.pdf';
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    // A detached anchor saves nothing in Firefox, and revoking in the same tick can beat the save.
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(() => URL.revokeObjectURL(url), 0);
  }

  isAtLeast(status: string): boolean {
    const order = this.order();
    if (!order) return false;
    return isAtLeastFn(order.status, status);
  }
}
