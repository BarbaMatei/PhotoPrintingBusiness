import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';

@Component({
  selector: 'app-checkout-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="checkout-shell">
      <div class="checkout-stepper">
        <a
          routerLink="livrare"
          routerLinkActive="active"
          class="step"
          ariaCurrentWhenActive="page"
        >
          <span class="step-number">1</span>
          <span class="step-label">Livrare</span>
        </a>
        <span class="step-divider"></span>
        <a
          routerLink="recapitulare"
          routerLinkActive="active"
          [class.disabled]="!checkoutState.isDeliveryComplete()"
          class="step"
          ariaCurrentWhenActive="page"
        >
          <span class="step-number">2</span>
          <span class="step-label">Recapitulare</span>
        </a>
        <span class="step-divider"></span>
        <a
          routerLink="plata"
          routerLinkActive="active"
          [class.disabled]="!checkoutState.isDeliveryComplete()"
          class="step"
          ariaCurrentWhenActive="page"
        >
          <span class="step-number">3</span>
          <span class="step-label">Plată</span>
        </a>
      </div>
      <div class="checkout-content">
        <router-outlet />
      </div>
    </div>
  `,
  styles: [`
    .checkout-shell {
      max-width: 860px;
      margin: 0 auto;
      padding: 1.5rem 1rem;
    }

    .checkout-stepper {
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 2rem;
      gap: 0.5rem;
    }

    .step {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      text-decoration: none;
      color: #6c757d;
      font-size: 0.95rem;
      transition: color 0.2s;

      &.active {
        color: #1a73e8;
        font-weight: 600;

        .step-number {
          background: #1a73e8;
          color: #fff;
        }
      }

      &.disabled {
        pointer-events: none;
        opacity: 0.45;
      }
    }

    .step-number {
      width: 28px;
      height: 28px;
      border-radius: 50%;
      background: #e9ecef;
      color: #495057;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.85rem;
      font-weight: 600;
    }

    .step-divider {
      flex: 1;
      height: 1px;
      background: #dee2e6;
      max-width: 60px;
    }
  `],
})
export class CheckoutShell {
  readonly checkoutState = inject(CheckoutStateService);
}
