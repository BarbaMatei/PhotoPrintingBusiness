import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminService } from '../../../../core/services/admin.service';
import { AdminHubService } from '../../../../core/services/admin-hub.service';
import { AdminOrderSummaryDto } from '../../../../core/models/admin.model';
import { STATUS_LABELS, statusClass } from '../../../../core/models/order-status.constants';

const STATUS_OPTIONS = ['', 'AwaitingPayment', 'Paid', 'Printing', 'Shipped', 'Delivered', 'Cancelled', 'PaymentFailed'];

@Component({
  selector: 'app-admin-orders-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-orders-page.html',
  styleUrl: './admin-orders-page.scss',
})
export class AdminOrdersPage implements OnInit, OnDestroy {
  private readonly adminSvc = inject(AdminService);
  private readonly hubSvc = inject(AdminHubService);
  private readonly cdr = inject(ChangeDetectorRef);

  orders: AdminOrderSummaryDto[] = [];
  total = 0;
  loading = true;
  error: string | null = null;

  page = 1;
  pageSize = 20;
  statusFilter = '';
  searchQuery = '';

  readonly statusOptions = STATUS_OPTIONS;
  readonly statusLabel = (s: string) => s ? (STATUS_LABELS[s] ?? s) : 'Toate statusurile';
  readonly statusClass = statusClass;

  private subs = new Subscription();
  private searchTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.load();

    this.subs.add(
      this.hubSvc.newOrderReceived$.subscribe(event => {
        this.orders = [
          {
            id: event.id,
            orderNumber: event.orderNumber,
            status: event.status,
            customerEmail: event.customerEmail,
            customerName: event.customerName,
            totalRon: event.totalRon,
            createdAt: event.createdAt,
            itemCount: 0,
            deliveryType: '',
          },
          ...this.orders,
        ];
        this.total++;
        this.cdr.markForCheck();
      })
    );

    this.subs.add(
      this.hubSvc.orderStatusChanged$.subscribe(({ orderId, status }) => {
        const order = this.orders.find(o => o.id === orderId);
        if (order) {
          order.status = status;
          this.cdr.markForCheck();
        }
      })
    );

    this.hubSvc.connect().catch(err => console.warn('Admin hub connect failed:', err));
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.cdr.markForCheck();

    this.adminSvc.getOrders(this.page, this.pageSize, this.statusFilter || undefined, this.searchQuery || undefined).subscribe({
      next: result => {
        this.orders = result.items;
        this.total = result.total;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'Eroare la încărcarea comenzilor.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  onSearchChange(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 300);
  }

  onStatusChange(): void {
    this.page = 1;
    this.load();
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.load();
    }
  }

  nextPage(): void {
    if (this.page * this.pageSize < this.total) {
      this.page++;
      this.load();
    }
  }

  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('ro-RO', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  formatRon(value: number): string {
    return value.toFixed(2).replace('.', ',') + ' RON';
  }
}
