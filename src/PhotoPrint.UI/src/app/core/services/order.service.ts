import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OrderDetailDto, OrderPhotosDto, OrderSummaryDto } from '../models/order.model';

export interface OrdersPage {
  items: OrderSummaryDto[];
  total: number;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/orders`;

  getOrders(page = 1, pageSize = 10): Observable<OrdersPage> {
    return this.http
      .get<{ items: OrderSummaryDto[]; total: number; page: number; size: number }>(this.base, {
        params: { page: String(page), pageSize: String(pageSize) },
      })
      .pipe(map(r => ({ items: r.items, total: r.total })));
  }

  getOrderDetail(id: string): Observable<OrderDetailDto> {
    return this.http.get<OrderDetailDto>(`${this.base}/${id}`);
  }

  /** Bolt 053: order's photo archive — presigned cloud URLs, 1h TTL. */
  getOrderPhotos(id: string): Observable<OrderPhotosDto> {
    return this.http.get<OrderPhotosDto>(`${this.base}/${id}/photos`);
  }
}
