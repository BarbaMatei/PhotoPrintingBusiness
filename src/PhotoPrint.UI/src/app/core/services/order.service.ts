import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { BaseApiService } from './api/base-api.service';
import { OrderDetailDto, OrderPhotosDto, OrderSummaryDto } from '../models/order.model';

export interface OrdersPage {
  items: OrderSummaryDto[];
  total: number;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly api = inject(BaseApiService);
  private readonly base = '/orders';

  getOrders(page = 1, pageSize = 10): Observable<OrdersPage> {
    return this.api
      .get<{ items: OrderSummaryDto[]; total: number; page: number; size: number }>(this.base, {
        params: { page, pageSize },
      })
      .pipe(map(r => ({ items: r.items, total: r.total })));
  }

  getOrderDetail(id: string): Observable<OrderDetailDto> {
    return this.api.get<OrderDetailDto>(`${this.base}/${id}`);
  }

  /** Bolt 053: order's photo archive — presigned cloud URLs, 1h TTL. */
  getOrderPhotos(id: string): Observable<OrderPhotosDto> {
    return this.api.get<OrderPhotosDto>(`${this.base}/${id}/photos`);
  }
}
