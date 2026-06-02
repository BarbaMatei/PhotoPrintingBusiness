import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  input,
  OnInit,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { catchError, EMPTY, of } from 'rxjs';
import { OrderService } from '../../../core/services/order.service';
import { OrderStatusPipe } from '../../../core/pipes/order-status.pipe';
import { statusClass, isAtLeast } from '../../../core/models/order-status.constants';
import { OrderDetailDto, OrderPhotoDto } from '../../../core/models/order.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { PhotoLightboxComponent } from '../../../shared/components/photo-lightbox/photo-lightbox.component';

interface StepDef {
  status: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-order-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, RouterLink, OrderStatusPipe, SpinnerComponent, PhotoLightboxComponent],
  template: `
    <div class="order-detail-page">
      @if (loading()) {
        <app-spinner label="Se încarcă comanda..." [showLabel]="true" />
      }

      @if (!loading() && order()) {
        <div class="order-detail">
          <a routerLink="/comenzile-mele" class="back-link">‹ Înapoi la comenzi</a>

          <div class="order-header">
            <h1>Comanda #{{ order()!.orderNumber }}</h1>
            <span class="status-badge" [class]="badgeClass(order()!.status)">
              {{ order()!.status | orderStatus }}
            </span>
          </div>

          <!-- Status stepper -->
          <div class="status-stepper">
            @for (step of steps; track step.status; let last = $last) {
              <div class="status-step" [class.done]="stepDone(order()!.status, step.status)">
                <div class="status-icon">{{ step.icon }}</div>
                <div class="status-label">{{ step.label }}</div>
              </div>
              @if (!last) {
                <div class="status-divider"></div>
              }
            }
          </div>

          <!-- Line items -->
          <section class="order-items">
            <h2>Fotografii comandate</h2>
            @for (item of order()!.items; track item.uploadId) {
              <div class="order-item">
                <img
                  [src]="item.previewUrl || '/assets/placeholder.png'"
                  [alt]="item.productName"
                  class="item-thumb"
                />
                <div class="item-info">
                  <div class="item-name">{{ item.productName }}</div>
                  <div class="item-meta">{{ item.size }} · {{ item.finish }}</div>
                  <div class="item-qty">× {{ item.quantity }}</div>
                </div>
                <div class="item-total">{{ item.lineTotalRon | number:'1.2-2' }} RON</div>
              </div>
            }
          </section>

          <!-- Cost summary -->
          <section class="cost-summary">
            <div class="summary-row">
              <span>Subtotal</span>
              <span>{{ order()!.subtotalRon | number:'1.2-2' }} RON</span>
            </div>
            <div class="summary-row">
              <span>Transport</span>
              <span>{{ order()!.shippingCostRon | number:'1.2-2' }} RON</span>
            </div>
            <div class="summary-row summary-row--total">
              <span>Total</span>
              <strong>{{ order()!.totalRon | number:'1.2-2' }} RON</strong>
            </div>
          </section>

          <!-- Photo archive (bolt 053) -->
          <section class="order-photos">
            <h2>Fotografiile tale</h2>

            @if (photosLoading()) {
              <app-spinner label="Se încarcă fotografiile..." [showLabel]="true" />
            } @else if (photos().length === 0) {
              <p class="photos-empty">
                Fotografiile pentru această comandă nu mai sunt disponibile.
              </p>
            } @else {
              <div class="photo-grid">
                @for (photo of photos(); track photo.uploadId) {
                  <button
                    type="button"
                    class="photo-tile"
                    (click)="openLightbox(photo)"
                    [attr.aria-label]="'Vezi ' + photo.fileName"
                  >
                    <img
                      [src]="photo.thumbnailUrl"
                      [alt]="photo.fileName"
                      loading="lazy"
                    />
                  </button>
                }
              </div>
            }
          </section>

          <!-- Delivery info -->
          <section class="delivery-info">
            <h2>Livrare</h2>
            @if (order()!.deliveryType === 'Easybox') {
              <p><strong>Easybox Sameday</strong></p>
              <p>{{ order()!.lockerName }}</p>
              <p>{{ order()!.lockerAddress }}</p>
            } @else {
              @if (order()!.shippingAddress; as addr) {
                <p>{{ addr.recipientName }}</p>
                <p>{{ addr.street }} {{ addr.number }}{{ addr.block ? ', bl. ' + addr.block : '' }}</p>
                <p>{{ addr.city }}, {{ addr.county }}, {{ addr.postalCode }}</p>
                <p>{{ addr.phone }}</p>
              }
            }
          </section>
        </div>
      }

      <!-- Lightbox overlay — mounted only when a photo is selected. The large URL
           is requested by the browser at this point (lazy-load per story 002). -->
      <app-photo-lightbox
        [src]="lightboxSrc()"
        (close)="lightboxSrc.set(null)"
      />
    </div>
  `,
  styles: [`
    .order-detail-page {
      max-width: 800px;
      margin: 2rem auto;
      padding: 1rem;
    }

    // .state-loading removed — replaced by <app-spinner>

    .back-link {
      color: #1a73e8;
      text-decoration: none;
      font-size: 0.9rem;
      display: inline-block;
      margin-bottom: 1rem;
    }

    .order-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 1.5rem;
      h1 { font-size: 1.5rem; font-weight: 700; margin: 0; }
    }

    .status-badge {
      display: inline-block;
      padding: 0.2rem 0.6rem;
      border-radius: 12px;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .status--pending   { background: #fff3cd; color: #856404; }
    .status--paid      { background: #d1ecf1; color: #0c5460; }
    .status--printing  { background: #cce5ff; color: #004085; }
    .status--shipped   { background: #d4edda; color: #155724; }
    .status--delivered { background: #d4edda; color: #155724; }
    .status--cancelled { background: #f8d7da; color: #721c24; }

    .status-stepper {
      display: flex;
      align-items: center;
      margin-bottom: 2rem;
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

    .status-divider { flex: 1; height: 2px; background: #dee2e6; }

    .order-items {
      margin-bottom: 1.5rem;
      h2 { font-size: 1.1rem; font-weight: 600; margin-bottom: 1rem; }
    }

    .order-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid #dee2e6;
    }

    .item-thumb {
      width: 60px;
      height: 60px;
      object-fit: cover;
      border-radius: 4px;
      border: 1px solid #dee2e6;
    }

    .item-info { flex: 1; }
    .item-name { font-weight: 600; }
    .item-meta { font-size: 0.85rem; color: #6c757d; }
    .item-qty  { font-size: 0.85rem; color: #6c757d; }
    .item-total { font-weight: 600; white-space: nowrap; }

    .cost-summary {
      background: #f8f9fa;
      border-radius: 8px;
      padding: 1rem 1.25rem;
      margin-bottom: 1.5rem;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      padding: 0.35rem 0;
      font-size: 0.95rem;

      &--total {
        border-top: 1px solid #dee2e6;
        margin-top: 0.5rem;
        padding-top: 0.5rem;
        font-weight: 600;
      }
    }

    .delivery-info {
      h2 { font-size: 1.1rem; font-weight: 600; margin-bottom: 0.75rem; }
      p { margin: 0.25rem 0; font-size: 0.95rem; }
    }

    /* Photo archive section (bolt 053) */
    .order-photos {
      margin-bottom: 1.5rem;
      h2 { font-size: 1.1rem; font-weight: 600; margin-bottom: 1rem; }
    }

    .photos-empty {
      color: #6c757d;
      font-style: italic;
      margin: 0;
    }

    .photo-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
      gap: 0.75rem;
    }

    .photo-tile {
      all: unset;
      cursor: pointer;
      border-radius: 6px;
      overflow: hidden;
      aspect-ratio: 1 / 1;
      background: #f0f0f0;
      border: 1px solid #dee2e6;
      transition: transform 0.15s, box-shadow 0.15s;

      &:hover {
        transform: scale(1.02);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      }

      &:focus-visible {
        outline: 2px solid #1a73e8;
        outline-offset: 2px;
      }

      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
      }
    }
  `],
})
export class OrderDetailPage implements OnInit {
  readonly orderId = input.required<string>();

  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly order = signal<OrderDetailDto | null>(null);

  // Bolt 053: photo archive + lightbox
  readonly photosLoading = signal(true);
  readonly photos = signal<OrderPhotoDto[]>([]);
  readonly lightboxSrc = signal<string | null>(null);

  readonly badgeClass = statusClass;
  readonly stepDone = isAtLeast;

  readonly steps: StepDef[] = [
    { status: 'Paid', label: 'Plătită', icon: '✓' },
    { status: 'Printing', label: 'În tipărire', icon: '🖨' },
    { status: 'Shipped', label: 'Expediată', icon: '📦' },
    { status: 'Delivered', label: 'Livrată', icon: '🏠' },
  ];

  ngOnInit(): void {
    const id = this.orderId();

    this.orderService
      .getOrderDetail(id)
      .pipe(
        catchError(() => {
          this.router.navigate(['/comenzile-mele']);
          return EMPTY;
        })
      )
      .subscribe(order => {
        this.order.set(order);
        this.loading.set(false);
      });

    // Bolt 053: fetch photos in parallel. A failure here doesn't navigate away —
    // the empty-state copy ("no longer available") covers it gracefully.
    this.orderService
      .getOrderPhotos(id)
      .pipe(catchError(() => of({ photos: [] })))
      .subscribe(result => {
        this.photos.set(result.photos);
        this.photosLoading.set(false);
      });
  }

  openLightbox(photo: OrderPhotoDto): void {
    this.lightboxSrc.set(photo.largeUrl);
  }
}
