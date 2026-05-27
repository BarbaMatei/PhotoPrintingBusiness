import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  input,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { AdminService } from '../../../../core/services/admin.service';
import { AdminHubService } from '../../../../core/services/admin-hub.service';
import { AdminOrderDetailDto } from '../../../../core/models/admin.model';
import { STATUS_LABELS, statusClass } from '../../../../core/models/order-status.constants';
import { SpinnerComponent } from '../../../../shared/components/spinner/spinner.component';
import { BreadcrumbComponent } from '../../../../shared/components/breadcrumb/breadcrumb.component';

const NEXT_STATUSES: Record<string, string[]> = {
  Paid:     ['Printing', 'Cancelled'],
  Printing: ['Shipped',  'Cancelled'],
  Shipped:  ['Delivered'],
};

@Component({
  selector: 'app-admin-order-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SpinnerComponent, BreadcrumbComponent],
  templateUrl: './admin-order-detail-page.html',
  styleUrl: './admin-order-detail-page.scss',
})
export class AdminOrderDetailPage implements OnInit, OnDestroy {
  readonly orderId = input.required<string>();

  private readonly adminSvc = inject(AdminService);
  private readonly hubSvc = inject(AdminHubService);
  private readonly cdr = inject(ChangeDetectorRef);

  order: AdminOrderDetailDto | null = null;
  loading = true;
  error: string | null = null;
  actionError: string | null = null;
  actionLoading = false;

  // Status transition form state
  selectedStatus = '';
  awbNumber = '';
  trackingUrl = '';

  // Notes form state
  notesText = '';
  notesSaving = false;

  // Cancel modal
  showCancelModal = false;
  cancelReason = '';
  cancelLoading = false;

  readonly statusLabel = (s: string) => STATUS_LABELS[s] ?? s;
  readonly statusClass = statusClass;

  private subs = new Subscription();

  get nextStatuses(): string[] {
    return NEXT_STATUSES[this.order?.status ?? ''] ?? [];
  }

  get showAwbFields(): boolean {
    return this.selectedStatus === 'Shipped';
  }

  ngOnInit(): void {
    this.load();

    this.subs.add(
      this.hubSvc.orderStatusChanged$.subscribe(({ orderId, status }) => {
        if (orderId === this.orderId() && this.order) {
          this.order = { ...this.order, status };
          this.cdr.markForCheck();
        }
      })
    );

    this.hubSvc.connect().catch(err => console.warn('Admin hub connect failed:', err));
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.cdr.markForCheck();

    this.adminSvc.getOrderDetail(this.orderId()).subscribe({
      next: order => {
        this.order = order;
        this.notesText = order.internalNotes ?? '';
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'Comanda nu a putut fi încărcată.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  applyStatusChange(): void {
    if (!this.selectedStatus) return;

    this.actionLoading = true;
    this.actionError = null;
    this.cdr.markForCheck();

    this.adminSvc.updateOrderStatus(this.orderId(), {
      status: this.selectedStatus,
      awbNumber: this.awbNumber || null,
      trackingUrl: this.trackingUrl || null,
    }).subscribe({
      next: order => {
        this.order = order;
        this.selectedStatus = '';
        this.awbNumber = '';
        this.trackingUrl = '';
        this.actionLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.actionError = 'Actualizarea statusului a eșuat.';
        this.actionLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  downloadZip(): void {
    if (!this.order) return;
    this.adminSvc.downloadZip(this.order.id, this.order.orderNumber).subscribe({
      error: () => {
        this.actionError = 'Descărcarea a eșuat.';
        this.cdr.markForCheck();
      },
    });
  }

  openCancelModal(): void {
    this.cancelReason = '';
    this.showCancelModal = true;
    this.cdr.markForCheck();
  }

  closeCancelModal(): void {
    this.showCancelModal = false;
    this.cdr.markForCheck();
  }

  doCancel(): void {
    this.cancelLoading = true;
    this.actionError = null;
    this.cdr.markForCheck();

    this.adminSvc.cancelOrder(this.orderId(), this.cancelReason || undefined).subscribe({
      next: order => {
        this.order = order;
        this.showCancelModal = false;
        this.cancelLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.actionError = 'Anularea a eșuat.';
        this.cancelLoading = false;
        this.showCancelModal = false;
        this.cdr.markForCheck();
      },
    });
  }

  saveNotes(): void {
    this.notesSaving = true;
    this.cdr.markForCheck();

    this.adminSvc.updateOrderNotes(this.orderId(), { notes: this.notesText || null }).subscribe({
      next: order => {
        this.order = order;
        this.notesSaving = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notesSaving = false;
        this.cdr.markForCheck();
      },
    });
  }

  formatDate(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('ro-RO', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  formatRon(value: number): string {
    return value.toFixed(2).replace('.', ',') + ' RON';
  }
}
