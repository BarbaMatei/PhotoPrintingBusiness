import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { UploadService } from '../../../core/services/upload.service';
import { CartResponseDto, CartItemDto, CartGroupDto } from '../../../core/models/cart.model';

@Component({
  selector: 'app-cart-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, RouterLink],
  template: `
    <div class="cart-page">
      <h1 class="cart-page__title">Coșul tău 🛒</h1>

      @if (loading) {
        <p class="cart-page__loading">Se încarcă coșul…</p>
      } @else if (!cart || cart.groups.length === 0) {
        <div class="cart-page__empty">
          <div class="cart-empty-icon">🛒</div>
          <h2>Coșul tău este gol</h2>
          <p>Adaugă fotografii pentru a le tipări.</p>
          <a routerLink="/tipareste" class="btn btn--primary btn--lg">Tipărește fotografii</a>
        </div>
      } @else {
        <div class="cart-page__layout">
          <div class="cart-page__items">

            <!-- One card per product+size group -->
            @for (group of cart.groups; track group.productId + group.sizeId + (group.finishName ?? '')) {
              <div class="order-group">
                <div class="order-group__header">
                  <div class="order-group__info">
                    <span class="order-group__product">📷 {{ group.productName }}</span>
                    <span class="order-group__size">{{ group.sizeName }}</span>
                    @if (group.finishName) {
                      <span class="order-group__finish">{{ group.finishName }}</span>
                    }
                  </div>
                  <span class="order-group__count">{{ group.items.length }} foto</span>
                </div>

                <div class="order-group__body">
                  <!-- Summary row: total copies × unit price (tier-based) -->
                  <div class="order-group__summary-row">
                    <span>{{ group.totalCopies }} copii × {{ group.unitPrice | number:'1.2-2' }} lei/buc</span>
                    <strong>{{ group.subtotal | number:'1.2-2' }} lei</strong>
                  </div>

                  <!-- Expandable photo list -->
                  <details class="order-group__photos">
                    <summary class="order-group__photos-toggle">
                      Arată toate fotografiile ({{ group.items.length }})
                    </summary>
                    <div class="photo-list">
                      @for (item of group.items; track item.uploadId) {
                        <div class="photo-list__item">
                          <img
                            [src]="blobUrls.get(item.uploadId) || ''"
                            [alt]="'Fotografie'"
                            class="photo-list__thumb"
                            loading="lazy"
                          />
                          <div class="photo-list__meta">
                            <span class="photo-list__dims">{{ item.widthPx }} × {{ item.heightPx }} px</span>
                            <span class="photo-list__qty">Cantitate: {{ item.quantity }}</span>
                          </div>
                          <span class="photo-list__price">{{ item.lineTotal | number:'1.2-2' }} lei</span>
                          <button class="photo-list__remove" (click)="onRemoveItem(group, item)" title="Șterge">✕</button>
                        </div>
                      }
                    </div>
                  </details>
                </div>
              </div>
            }

            <div class="cart-page__actions-bottom">
              <a routerLink="/tipareste" class="btn btn--secondary">
                + Adaugă alt format
              </a>
            </div>

          </div>

          <aside class="cart-page__summary">
            <div class="cart-summary">
              <h3 class="cart-summary__title">Rezumat</h3>
              @for (group of cart.groups; track group.productId + group.sizeId + (group.finishName ?? '')) {
                <div class="cart-summary__row">
                  <span>{{ group.productName }} – {{ group.sizeName }} ({{ group.totalCopies }} copii)</span>
                  <span>{{ group.subtotal | number:'1.2-2' }} lei</span>
                </div>
              }
              <div class="cart-summary__row cart-summary__row--shipping">
                <span>Livrare</span>
                <span>Calculat la pasul următor</span>
              </div>
              <div class="cart-summary__row cart-summary__row--total">
                <strong>Total</strong>
                <strong>{{ cart.subtotal | number:'1.2-2' }} lei</strong>
              </div>
              <div class="cart-summary__actions">
                <a routerLink="/checkout" class="btn-checkout">
                  Finalizează comanda →
                </a>
              </div>
            </div>
          </aside>
        </div>
      }
    </div>
  `,
  styles: [`
    .cart-page {
      max-width: 960px;
      margin: 0 auto;
      padding: 2rem 1rem;
    }

    .cart-page__title {
      font-size: 1.75rem;
      font-weight: 700;
      margin-bottom: 1.5rem;
      color: #202124;
    }

    .cart-page__empty {
      text-align: center;
      padding: 4rem 0;

      .cart-empty-icon { font-size: 4rem; margin-bottom: 1rem; }
      h2 { font-size: 1.25rem; margin-bottom: 0.5rem; }
      p { color: #5f6368; margin-bottom: 1.5rem; }
    }

    .cart-page__layout {
      display: flex;
      gap: 2rem;
      align-items: flex-start;

      @media (max-width: 700px) { flex-direction: column; }
    }

    .cart-page__items { flex: 1; display: flex; flex-direction: column; gap: 1rem; }

    /* Order group card */
    .order-group {
      border: 1px solid #e8eaed;
      border-radius: 16px;
      overflow: hidden;
      background: #fff;
      box-shadow: 0 1px 4px rgba(0,0,0,.05);
    }

    .order-group__header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.85rem 1.25rem;
      background: linear-gradient(135deg, #e8f0fe 0%, #d2e3fc 100%);
      border-bottom: 1px solid #c5d8f7;
    }

    .order-group__info { display: flex; flex-direction: column; gap: 2px; }
    .order-group__product { font-weight: 700; font-size: 0.95rem; color: #202124; }
    .order-group__size { font-size: 0.8rem; color: #3c4043; font-weight: 500; }
    .order-group__finish {
      display: inline-block;
      padding: 0.1rem 0.5rem;
      background: #fff;
      border-radius: 20px;
      font-size: 0.75rem;
      color: #1558b0;
      font-weight: 600;
      width: fit-content;
    }
    .order-group__count { font-size: 0.85rem; font-weight: 600; color: #1558b0; }

    .order-group__body { padding: 1rem 1.25rem; }

    .order-group__summary-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.95rem;
      padding: 0.35rem 0;
      color: #3c4043;

      strong { font-size: 1.05rem; color: #1a73e8; }
    }

    .order-group__photos {
      margin-top: 0.75rem;

      .order-group__photos-toggle {
        font-size: 0.82rem;
        color: #1a73e8;
        cursor: pointer;
        font-weight: 500;
        user-select: none;
        &:hover { text-decoration: underline; }
      }
    }

    /* Photo list (inside details) */
    .photo-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      margin-top: 0.75rem;
      max-height: 320px;
      overflow-y: auto;
      padding-right: 0.25rem;

      &::-webkit-scrollbar { width: 4px; }
      &::-webkit-scrollbar-thumb { background: #dadce0; border-radius: 2px; }
    }

    .photo-list__item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.5rem;
      border-radius: 8px;
      background: #f8f9fa;
    }

    .photo-list__thumb {
      width: 56px;
      height: 56px;
      object-fit: cover;
      border-radius: 6px;
      flex-shrink: 0;
    }

    .photo-list__meta {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .photo-list__dims { font-size: 0.78rem; color: #5f6368; }
    .photo-list__qty { font-size: 0.82rem; font-weight: 500; color: #3c4043; }
    .photo-list__price { font-weight: 600; font-size: 0.9rem; white-space: nowrap; }

    .photo-list__remove {
      background: none;
      border: none;
      cursor: pointer;
      color: #9aa0a6;
      font-size: 1rem;
      padding: 0.25rem;
      border-radius: 4px;
      transition: color 0.15s, background 0.15s;

      &:hover { color: #d93025; background: #fce8e6; }
    }

    .cart-page__actions-bottom { margin-top: 0.25rem; }

    /* Summary panel */
    .cart-page__summary { width: 280px; flex-shrink: 0; position: sticky; top: 1rem; }

    .cart-summary {
      border: 1px solid #e8eaed;
      border-radius: 16px;
      padding: 1.25rem;
      background: #fff;
      box-shadow: 0 2px 8px rgba(0,0,0,.06);
    }

    .cart-summary__title {
      font-size: 1rem;
      font-weight: 700;
      margin: 0 0 1rem;
      color: #202124;
    }

    .cart-summary__row {
      display: flex;
      justify-content: space-between;
      padding: 0.35rem 0;
      font-size: 0.9rem;
      color: #3c4043;
    }

    .cart-summary__row--shipping { color: #5f6368; font-size: 0.8rem; }

    .cart-summary__row--total {
      border-top: 1px solid #e8eaed;
      padding-top: 0.75rem;
      margin-top: 0.25rem;
      font-size: 1rem;

      strong { color: #1a73e8; }
    }

    .cart-summary__actions { margin-top: 1rem; }

    .btn-checkout {
      display: block;
      width: 100%;
      padding: 0.9rem 1.5rem;
      border-radius: 12px;
      border: none;
      font-size: 1rem;
      font-weight: 700;
      text-align: center;
      text-decoration: none;
      background: linear-gradient(135deg, #ff6d00 0%, #ff9100 100%);
      color: #fff;
      box-shadow: 0 4px 14px rgba(255,109,0,.35);
      transition: transform 0.15s, box-shadow 0.15s;
      cursor: pointer;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(255,109,0,.45);
        text-decoration: none;
      }
    }
  `],
})
export class CartPage implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly uploadService = inject(UploadService);
  private readonly cdr = inject(ChangeDetectorRef);

  cart: CartResponseDto | null = null;
  loading = true;
  /** Maps uploadId → blob object URL for authenticated preview display. */
  readonly blobUrls = new Map<string, string>();

  ngOnInit(): void {
    this.cartService.cart$.subscribe(cart => {
      this.cart = cart;
      this.loading = false;
      this.cdr.markForCheck();

      // Fetch blob URLs for any item not yet in the map.
      cart?.groups.forEach(group =>
        group.items.forEach(item => {
          if (!this.blobUrls.has(item.uploadId)) {
            this.uploadService.getPreviewBlob(item.uploadId).subscribe({
              next: url => {
                this.blobUrls.set(item.uploadId, url);
                this.cdr.markForCheck();
              },
              error: () => { /* preview unavailable — show broken img gracefully */ },
            });
          }
        }),
      );
    });
  }

  onRemoveItem(group: CartGroupDto, item: CartItemDto): void {
    if (!this.cart) return;
    const remaining = group.items.filter(i => i.uploadId !== item.uploadId);
    // Empty items array tells the backend to drop the whole group.
    // If this was the last group, the cart will become empty.
    this.cartService.setCart({
      productId: group.productId,
      sizeId: group.sizeId,
      finishName: group.finishName,
      items: remaining.map(i => ({ uploadId: i.uploadId, quantity: i.quantity })),
    }).subscribe();
  }
}


