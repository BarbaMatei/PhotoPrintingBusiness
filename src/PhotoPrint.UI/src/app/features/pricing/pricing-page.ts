import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { Product, PricingTier } from '../../core/models/product.model';

@Component({
  selector: 'app-pricing-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, RouterLink],
  template: `
    <div class="pricing-page">

      <!-- Header -->
      <header class="pricing-hero">
        <div class="container">
          <h1 class="pricing-hero__title">Formate &amp; Prețuri</h1>
          <p class="pricing-hero__sub">
            Toate prețurile includ TVA. Cu cât tipărești mai mult, cu atât prețul per bucată scade.
          </p>
        </div>
      </header>

      <!-- Loading -->
      @if (loading()) {
        <div class="container pricing-loading">
          <div class="skeleton skeleton--title"></div>
          <div class="skeleton skeleton--body"></div>
          <div class="skeleton skeleton--body"></div>
        </div>
      }

      <!-- Error -->
      @if (error()) {
        <div class="container pricing-error">
          <p>Nu am putut încărca prețurile. <button class="btn btn--ghost btn--sm" (click)="load()">Încearcă din nou</button></p>
        </div>
      }

      <!-- Products -->
      @if (!loading() && !error()) {
        <div class="container">
          @for (product of products(); track product.id) {
            <section class="product-section">
              <h2 class="product-section__name">{{ product.name }}</h2>
              @if (product.finishes.length > 0) {
                <p class="product-section__finishes">
                  Finisaje disponibile:
                  @for (f of product.finishes; track f; let last = $last) {
                    <strong>{{ f }}</strong>@if (!last) { , }
                  }
                </p>
              }
              <div class="size-grid">
                @for (size of product.sizes; track size.id) {
                  <div class="size-card">
                    <div class="size-card__label">{{ size.label }}</div>
                    <div class="size-card__dim">{{ size.widthMm }}&thinsp;×&thinsp;{{ size.heightMm }}&thinsp;mm</div>
                    <table class="tier-table">
                      <thead>
                        <tr>
                          <th>Cantitate</th>
                          <th>Preț / buc</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (tier of size.pricingTiers; track tier.minQuantity) {
                          <tr>
                            <td>{{ tierLabel(tier) }}</td>
                            <td class="tier-table__price">{{ tier.unitPrice | number:'1.2-2' }} lei</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                }
              </div>
            </section>
          }

          <!-- CTA -->
          <div class="pricing-cta">
            <a routerLink="/tipareste" class="btn btn--accent btn--xl">
              Tipărește acum
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
            </a>
          </div>
        </div>
      }

    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .pricing-page {
      min-height: 60vh;
      padding-bottom: $space-16;
    }

    .pricing-hero {
      background: $color-bg-soft;
      border-bottom: 1px solid $color-neutral-300;
      padding: $space-12 0 $space-10;
      text-align: center;
    }

    .pricing-hero__title {
      font-size: clamp(1.8rem, 4vw, 2.6rem);
      font-weight: 800;
      color: $color-neutral-900;
      margin: 0 0 $space-3;
    }

    .pricing-hero__sub {
      font-size: $font-size-base;
      color: $color-neutral-500;
      margin: 0;
    }

    .pricing-loading,
    .pricing-error {
      padding: $space-12 0;
      display: flex;
      flex-direction: column;
      gap: $space-4;
    }

    .pricing-error {
      color: $color-neutral-500;
      text-align: center;
    }

    /* Product section */
    .product-section {
      margin-top: $space-12;
    }

    .product-section__name {
      font-size: 1.3rem;
      font-weight: 700;
      color: $color-neutral-900;
      margin: 0 0 $space-1;
      padding-bottom: $space-3;
      border-bottom: 2px solid $color-primary;
      display: inline-block;
    }

    .product-section__finishes {
      font-size: $font-size-sm;
      color: $color-neutral-500;
      margin: $space-2 0 $space-5;
    }

    /* Size grid */
    .size-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: $space-4;
      margin-top: $space-5;
    }

    .size-card {
      background: $color-white;
      border: 1px solid $color-neutral-300;
      border-radius: $radius-xl;
      padding: $space-5;
      transition: box-shadow 0.2s, transform 0.2s;

      &:hover {
        box-shadow: $shadow-md;
        transform: translateY(-2px);
      }
    }

    .size-card__label {
      font-size: 1.05rem;
      font-weight: 700;
      color: $color-neutral-900;
      margin-bottom: $space-1;
    }

    .size-card__dim {
      font-size: $font-size-xs;
      color: $color-neutral-500;
      margin-bottom: $space-4;
    }

    /* Tier table */
    .tier-table {
      width: 100%;
      border-collapse: collapse;
      font-size: $font-size-sm;

      th {
        text-align: left;
        font-weight: 600;
        color: $color-neutral-500;
        font-size: $font-size-xs;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        padding-bottom: $space-2;
        border-bottom: 1px solid $color-neutral-300;
      }

      td {
        padding: $space-2 0;
        color: $color-neutral-700;
        border-bottom: 1px solid $color-neutral-100;

        &:last-child { border-bottom: none; }
      }
    }

    .tier-table__price {
      font-weight: 700;
      color: $color-primary;
      text-align: right;
    }

    /* CTA */
    .pricing-cta {
      text-align: center;
      margin-top: $space-16;
    }
  `],
})
export class PricingPage implements OnInit {
  private readonly productService = inject(ProductService);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    this.productService.getCatalog().subscribe({
      next: (products) => {
        this.products.set(products.filter(p => p.sizes.length > 0));
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  tierLabel(tier: PricingTier): string {
    if (tier.maxQuantity === null) {
      return `${tier.minQuantity}+ buc`;
    }
    return `${tier.minQuantity}–${tier.maxQuantity} buc`;
  }
}
