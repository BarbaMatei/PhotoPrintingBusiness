import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Product } from '../../../core/models/product.model';
import { lowestPrice } from '../../utils/pricing.utils';

@Component({
  selector: 'app-product-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe],
  template: `
    <a class="product-card" [routerLink]="['/tipareste', product.id]">
      <div class="product-card__image">
        @if (product.imageUrl) {
          <img [src]="product.imageUrl" [alt]="product.name" />
        } @else {
          <div class="product-card__image-placeholder">
            <span>📷</span>
          </div>
        }
      </div>
      <div class="product-card__body">
        <h3 class="product-card__name">{{ product.name }}</h3>
        @if (product.sizes.length > 0) {
          <p class="product-card__sizes">
            {{ sizeLabels }}
          </p>
        }
        @if (minPrice !== null) {
          <p class="product-card__price">de la <strong>{{ minPrice | number:'1.2-2' }} lei</strong></p>
        }
      </div>
    </a>
  `,
  styleUrl: './product-card.scss',
})
export class ProductCardComponent {
  @Input({ required: true }) product!: Product;

  get sizeLabels(): string {
    return this.product.sizes.map(s => s.label).join(', ');
  }

  get minPrice(): number | null {
    return lowestPrice(this.product.sizes);
  }
}
