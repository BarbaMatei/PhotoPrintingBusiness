import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
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
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/admin`;

  // ── Stats ──────────────────────────────────────────────────────────────────

  getStats(): Observable<AdminStatsDto> {
    return this.http.get<AdminStatsDto>(`${this.base}/stats/summary`);
  }

  getRevenueChart(days = 30): Observable<RevenueDataPointDto[]> {
    return this.http.get<RevenueDataPointDto[]>(`${this.base}/stats/revenue`, {
      params: { days: String(days) },
    });
  }

  getProductStats(): Observable<ProductStatsDto[]> {
    return this.http.get<ProductStatsDto[]>(`${this.base}/stats/products`);
  }

  getOrdersByStatus(): Observable<OrdersByStatusDto[]> {
    return this.http.get<OrdersByStatusDto[]>(`${this.base}/stats/orders-by-status`);
  }

  // ── Orders ─────────────────────────────────────────────────────────────────

  getOrders(
    page = 1,
    pageSize = 20,
    status?: string,
    search?: string,
  ): Observable<AdminOrdersPage> {
    const params: Record<string, string> = {
      page: String(page),
      pageSize: String(pageSize),
    };
    if (status) params['status'] = status;
    if (search) params['search'] = search;

    return this.http
      .get<{ items: AdminOrderSummaryDto[]; total: number }>(`${this.base}/orders`, { params })
      .pipe(map(r => ({ items: r.items, total: r.total })));
  }

  getOrderDetail(orderId: string): Observable<AdminOrderDetailDto> {
    return this.http.get<AdminOrderDetailDto>(`${this.base}/orders/${orderId}`);
  }

  updateOrderStatus(orderId: string, req: UpdateOrderStatusRequest): Observable<AdminOrderDetailDto> {
    return this.http.patch<AdminOrderDetailDto>(`${this.base}/orders/${orderId}/status`, req);
  }

  cancelOrder(orderId: string, reason?: string): Observable<AdminOrderDetailDto> {
    return this.http.post<AdminOrderDetailDto>(
      `${this.base}/orders/${orderId}/cancel`,
      { reason: reason ?? null }
    );
  }

  updateOrderNotes(orderId: string, req: UpdateOrderNotesRequest): Observable<AdminOrderDetailDto> {
    return this.http.patch<AdminOrderDetailDto>(`${this.base}/orders/${orderId}/notes`, req);
  }

  downloadZip(orderId: string, orderNumber: string): Observable<void> {
    const url = `${this.base}/orders/${orderId}/download-zip`;
    return this.http.get(url, { responseType: 'blob' }).pipe(
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
