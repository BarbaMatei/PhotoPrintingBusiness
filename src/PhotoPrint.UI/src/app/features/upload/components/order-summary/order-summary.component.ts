import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { UploadState } from '../../../../core/models/upload.model';
import { ProductSize, PricingTier } from '../../../../core/models/product.model';

@Component({
  selector: 'app-order-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    <div class="summary">
      <h3 class="summary__title">Sumar comandă</h3>

      <!-- Photo count + price row -->
      @if (doneCount === 0) {
        <p class="summary__empty">Nicio fotografie adăugată încă.</p>
      } @else {
        <div class="summary__row">
          <span class="summary__label">{{ totalCopies }} {{ totalCopies === 1 ? 'copie' : 'copii' }}</span>
          <span class="summary__qty-price">
            × {{ unitPrice | number:'1.2-2' }} lei/buc
          </span>
        </div>
        @if (tierLabel) {
          <div class="summary__tier-badge">🏷 Preț volum: {{ tierLabel }} buc</div>
        }
        <div class="summary__total">
          Total: <strong>{{ subtotal | number:'1.2-2' }} lei</strong>
        </div>
      }

      <!-- Pricing tier table -->
      @if (selectedSize && selectedSize.pricingTiers.length > 0) {
        <details class="summary__tiers">
          <summary class="summary__tiers-toggle">Vezi prețuri per volum</summary>
          <table class="tier-table">
            <thead>
              <tr><th>Cantitate</th><th>Preț/buc</th></tr>
            </thead>
            <tbody>
              @for (t of selectedSize.pricingTiers; track t.minQuantity) {
                <tr [class.tier-table__row--active]="isActiveTier(t)">
                  <td>
                    {{ t.minQuantity }}{{ t.maxQuantity !== null ? '–' + t.maxQuantity : '+' }} foto
                  </td>
                  <td class="tier-table__price">{{ t.unitPrice | number:'1.2-2' }} lei</td>
                </tr>
              }
            </tbody>
          </table>
        </details>
      }

      <!-- CTA button with tooltip for disabled state -->
      <div
        class="summary__btn-wrap"
        [attr.title]="canAddToCart ? null : disabledReason"
      >
        <button
          class="btn-cta"
          [disabled]="!canAddToCart"
          (click)="addToCart.emit()"
          aria-label="Adaugă în coș"
        >
          🛒 Adaugă în coș
        </button>
      </div>
    </div>
  `,
  styles: [`
    .summary {
      padding: 1.25rem;
      border: 1px solid #e8eaed;
      border-radius: 16px;
      background: #fff;
      box-shadow: 0 2px 8px rgba(0,0,0,.06);
    }

    .summary__title {
      margin: 0 0 1rem;
      font-size: 1rem;
      font-weight: 700;
      color: #202124;
    }

    .summary__empty {
      font-size: 0.875rem;
      color: #5f6368;
      margin-bottom: 0.75rem;
    }

    .summary__row {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      font-size: 0.9rem;
      padding: 0.25rem 0;
    }

    .summary__label { font-weight: 600; color: #202124; }
    .summary__qty-price { color: #5f6368; font-size: 0.85rem; }

    .summary__tier-badge {
      display: inline-block;
      margin-top: 0.25rem;
      padding: 0.2rem 0.6rem;
      background: linear-gradient(90deg, #e8f0fe, #d2e3fc);
      color: #1558b0;
      border-radius: 20px;
      font-size: 0.75rem;
      font-weight: 600;
    }

    .summary__total {
      font-size: 0.95rem;
      border-top: 1px solid #e8eaed;
      padding-top: 0.6rem;
      margin: 0.75rem 0;
      color: #202124;

      strong { font-size: 1.15rem; color: #1a73e8; }
    }

    /* Pricing tier table */
    .summary__tiers {
      margin-bottom: 1rem;
      summary.summary__tiers-toggle {
        font-size: 0.8rem;
        color: #1a73e8;
        cursor: pointer;
        user-select: none;
        font-weight: 500;
        &:hover { text-decoration: underline; }
      }
    }

    .tier-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 0.5rem;
      font-size: 0.8rem;

      th {
        text-align: left;
        padding: 0.3rem 0.5rem;
        background: #f8f9fa;
        color: #5f6368;
        font-weight: 600;
        border-bottom: 1px solid #e8eaed;
      }

      td { padding: 0.3rem 0.5rem; border-bottom: 1px solid #f1f3f4; }

      &__price { font-weight: 600; text-align: right; }

      &__row--active {
        background: #e8f0fe;
        td { color: #1558b0; font-weight: 600; }
      }
    }

    /* CTA button */
    .summary__btn-wrap { display: block; }

    .btn-cta {
      width: 100%;
      padding: 0.85rem 1.5rem;
      border: none;
      border-radius: 12px;
      font-size: 1rem;
      font-weight: 700;
      letter-spacing: 0.02em;
      cursor: pointer;
      background: linear-gradient(135deg, #ff6d00 0%, #ff9100 100%);
      color: #fff;
      box-shadow: 0 4px 14px rgba(255,109,0,.35);
      transition: transform 0.15s, box-shadow 0.15s, filter 0.15s;

      &:hover:not(:disabled) {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(255,109,0,.45);
        filter: brightness(1.06);
      }

      &:active:not(:disabled) { transform: translateY(0); }

      &:disabled {
        background: #dadce0;
        color: #80868b;
        box-shadow: none;
        cursor: not-allowed;
      }
    }
  `],
})
export class OrderSummaryComponent {
  @Input() uploads: UploadState[] = [];
  @Input() selectedSize: ProductSize | null = null;
  @Input() unitPrice = 0;
  @Input() tierLabel: string | null = null;
  @Input() canAddToCart = false;
  @Input() disabledReason: string | null = null;
  @Output() addToCart = new EventEmitter<void>();

  get doneCount(): number {
    return this.uploads.filter(u => u.status === 'done').length;
  }

  get totalCopies(): number {
    return this.uploads
      .filter(u => u.status === 'done')
      .reduce((sum, u) => sum + u.quantity, 0);
  }

  get subtotal(): number {
    return this.uploads
      .filter(u => u.status === 'done')
      .reduce((sum, u) => sum + u.quantity * this.unitPrice, 0);
  }

  isActiveTier(t: PricingTier): boolean {
    const total = this.uploads
      .filter(u => u.status === 'done')
      .reduce((s, u) => s + u.quantity, 0);
    return total >= t.minQuantity && (t.maxQuantity === null || total <= t.maxQuantity);
  }
}

