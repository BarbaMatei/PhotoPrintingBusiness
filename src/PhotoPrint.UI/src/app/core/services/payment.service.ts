import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateOrderRequest,
  StripeIntentResponse,
  OrderPaymentStatusDto,
} from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly paymentsBase = `${environment.apiUrl}/payments`;
  private readonly ordersBase = `${environment.apiUrl}/orders`;

  createStripeIntent(
    request: CreateOrderRequest,
    idempotencyKey?: string,
  ): Observable<StripeIntentResponse> {
    return this.http.post<StripeIntentResponse>(`${this.paymentsBase}/stripe/intent`, request, {
      headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {},
    });
  }

  getPaymentStatus(orderId: string): Observable<OrderPaymentStatusDto> {
    return this.http.get<OrderPaymentStatusDto>(`${this.ordersBase}/${orderId}/payment-status`);
  }

  // Fetched rather than linked: the endpoint authenticates a guest by header, which a plain
  // anchor cannot send.
  downloadInvoice(orderId: string): Observable<Blob> {
    return this.http.get(`${this.ordersBase}/${orderId}/invoice`, { responseType: 'blob' });
  }
}
