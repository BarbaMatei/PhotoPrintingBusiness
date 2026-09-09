import { Injectable, inject } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { BaseApiService } from './api/base-api.service';
import {
  AdminStatsDto,
  RevenueDataPointDto,
  ProductStatsDto,
  OrdersByStatusDto,
  AdminOrderSummaryDto,
  AdminOrderDetailDto,
  AdminOrdersPage,
  UpdateOrderStatusRequest,
  UpdateOrderNotesRequest,
} from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly api = inject(BaseApiService);
  private readonly base = '/admin';

  // ── Stats ──────────────────────────────────────────────────────────────────

  getStats(): Observable<AdminStatsDto> {
    return this.api.get<AdminStatsDto>(`${this.base}/stats/summary`);
  }

  getRevenueChart(days = 30): Observable<RevenueDataPointDto[]> {
    return this.api.get<RevenueDataPointDto[]>(`${this.base}/stats/revenue`, {
      params: { days },
    });
  }

  getProductStats(): Observable<ProductStatsDto[]> {
    return this.api.get<ProductStatsDto[]>(`${this.base}/stats/products`);
  }

  getOrdersByStatus(): Observable<OrdersByStatusDto[]> {
    return this.api.get<OrdersByStatusDto[]>(`${this.base}/stats/orders-by-status`);
  }

  // ── Orders ─────────────────────────────────────────────────────────────────

  getOrders(
    page = 1,
    pageSize = 20,
    status?: string,
    search?: string,
  ): Observable<AdminOrdersPage> {
    return this.api
      .get<{ items: AdminOrderSummaryDto[]; total: number }>(`${this.base}/orders`, {
        params: {
          page,
          pageSize,
          status: status || undefined,
          search: search || undefined,
        },
      })
      .pipe(map(r => ({ items: r.items, total: r.total })));
  }

  getOrderDetail(orderId: string): Observable<AdminOrderDetailDto> {
    return this.api.get<AdminOrderDetailDto>(`${this.base}/orders/${orderId}`);
  }

  updateOrderStatus(orderId: string, req: UpdateOrderStatusRequest): Observable<AdminOrderDetailDto> {
    return this.api.patch<AdminOrderDetailDto>(`${this.base}/orders/${orderId}/status`, req);
  }

  cancelOrder(orderId: string, reason?: string): Observable<AdminOrderDetailDto> {
    return this.api.post<AdminOrderDetailDto>(
      `${this.base}/orders/${orderId}/cancel`,
      { reason: reason ?? null }
    );
  }

  updateOrderNotes(orderId: string, req: UpdateOrderNotesRequest): Observable<AdminOrderDetailDto> {
    return this.api.patch<AdminOrderDetailDto>(`${this.base}/orders/${orderId}/notes`, req);
  }

  downloadZip(orderId: string, orderNumber: string): Observable<void> {
    return this.api.getBlob(`${this.base}/orders/${orderId}/download-zip`).pipe(
      tap(blob => {
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `comanda-${orderNumber}.zip`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(a.href);
      }),
      map(() => undefined),
    );
  }
}
