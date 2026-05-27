import {
  Component,
  inject,
  OnInit,
  ChangeDetectionStrategy,
  signal,
  input,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { isAtLeast as isAtLeastFn } from '../../../core/models/order-status.constants';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { CartService } from '../../../core/services/cart.service';
import { OrderDto } from '../../../core/models/payment.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';

@Component({
  selector: 'app-confirmation',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, RouterLink, SpinnerComponent],
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
          <div class="summary-row">
            <span>Total plătit:</span>
            <strong>{{ order()!.totalRon | number:'1.2-2' }} RON</strong>
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

      @if (!loading() && !order()) {
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

  readonly loading = signal(true);
  readonly order = signal<OrderDto | null>(null);

  readonly isAuthenticated = () => this.authService.isAuthenticated();

  ngOnInit(): void {
    this.paymentService.getOrder(this.orderId()).subscribe({
      next: order => {
        this.loading.set(false);
        if (order.status !== 'Paid' && order.status !== 'Printing' && order.status !== 'Shipped' && order.status !== 'Delivered') {
          // Order not in a success state — redirect home
          this.router.navigate(['/']);
          return;
        }
        this.order.set(order);
        // Reset checkout state now that we've confirmed success
        this.checkoutState.reset();
        this.cartService.clearCart().subscribe();
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  isAtLeast(status: string): boolean {
    const order = this.order();
    if (!order) return false;
    return isAtLeastFn(order.status, status);
  }
}
