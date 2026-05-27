import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { Router } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { OrderService } from '../../../core/services/order.service';
import { OrderStatusPipe } from '../../../core/pipes/order-status.pipe';
import { statusClass } from '../../../core/models/order-status.constants';
import { OrderSummaryDto } from '../../../core/models/order.model';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-order-history',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, DatePipe, OrderStatusPipe, SpinnerComponent, EmptyStateComponent],
  template: `
    <div class="order-history-page">
      <h1 class="page-title">Comenzile mele</h1>

      @if (loading()) {
        <app-spinner label="Se încarcă comenzile..." />
      }

      @if (!loading() && error()) {
        <p class="state-error">A apărut o eroare. Încearcă din nou.</p>
      }

      @if (!loading() && !error() && orders().length === 0) {
        <app-empty-state
          icon="📦"
          title="Nu ai nicio comandă încă."
          actionLabel="Tipărește fotografii"
          actionLink="/tipareste"
        />
      }

      @if (!loading() && !error() && orders().length > 0) {
        <div class="order-list">
          @for (order of orders(); track order.id) {
            <div
              class="order-row"
              role="button"
              [tabindex]="0"
              (click)="openDetail(order.id)"
              (keydown.enter)="openDetail(order.id)"
            >
              <div class="order-row__main">
                <span class="order-number">#{{ order.orderNumber }}</span>
                <span class="status-badge" [class]="badgeClass(order.status)">
                  {{ order.status | orderStatus }}
                </span>
              </div>
              <div class="order-row__meta">
                <span>{{ order.createdAt | date:'dd MMM yyyy' }}</span>
                <span>{{ order.itemCount }} foto</span>
                <span>{{ order.deliveryType === 'Easybox' ? 'Easybox' : 'Curier' }}</span>
              </div>
              <div class="order-row__total">{{ order.totalRon | number:'1.2-2' }} RON</div>
            </div>
          }
        </div>

        @if (totalPages() > 1) {
          <div class="pagination">
            <button class="btn" [disabled]="page() === 1" (click)="setPage(page() - 1)">
              ‹ Anterior
            </button>
            <span class="page-indicator">{{ page() }} / {{ totalPages() }}</span>
            <button class="btn" [disabled]="page() === totalPages()" (click)="setPage(page() + 1)">
              Următor ›
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .order-history-page {
      max-width: 800px;
      margin: $space-8 auto;
      padding: $space-4;
    }

    .page-title { font-size: $font-size-2xl; font-weight: $font-weight-bold; margin-bottom: $space-6; }

    .order-list { display: flex; flex-direction: column; gap: $space-3; }

    .order-row {
      border: 1px solid $color-neutral-300;
      border-radius: $radius-lg;
      padding: $space-4 $space-5;
      cursor: pointer;
      transition: box-shadow $transition-fast;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: $space-4;
      background: $color-white;

      &:hover { box-shadow: $shadow-md; }
    }

    .order-row__main {
      display: flex;
      align-items: center;
      gap: $space-3;
      flex: 1;
    }

    .order-number { font-weight: $font-weight-semi; }

    .order-row__meta {
      display: flex;
      gap: $space-4;
      color: $color-neutral-500;
      font-size: $font-size-sm;
    }

    .order-row__total { font-weight: $font-weight-semi; white-space: nowrap; }

    .status-badge {
      display: inline-block;
      padding: $space-1 $space-3;
      border-radius: $radius-full;
      font-size: $font-size-xs;
      font-weight: $font-weight-medium;
    }

    .status--pending   { background: $color-warning-light; color: #856404; }
    .status--paid      { background: $color-primary-light; color: $color-primary-dark; }
    .status--printing  { background: $color-primary-light; color: $color-primary-dark; }
    .status--shipped   { background: $color-success-light; color: $color-success; }
    .status--delivered { background: $color-success-light; color: $color-success; }
    .status--cancelled { background: $color-error-light;   color: $color-error; }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: $space-4;
      margin-top: $space-6;
    }

    .page-indicator { color: $color-neutral-500; }

  `],
})
export class OrderHistoryPage implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly page = signal(1);
  readonly total = signal(0);
  readonly orders = signal<OrderSummaryDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / PAGE_SIZE)));

  readonly badgeClass = statusClass;

  ngOnInit(): void {
    this.load();
  }

  setPage(p: number): void {
    this.page.set(p);
    this.load();
  }

  openDetail(id: string): void {
    this.router.navigate(['/comenzile-mele', id]);
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(false);
    this.orderService.getOrders(this.page(), PAGE_SIZE).subscribe({
      next: result => {
        this.orders.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
