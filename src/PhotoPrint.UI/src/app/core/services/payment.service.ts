import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateOrderRequest, StripeIntentResponse, OrderDto } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly paymentsBase = `${environment.apiUrl}/payments`;
  private readonly ordersBase = `${environment.apiUrl}/orders`;

  createStripeIntent(request: CreateOrderRequest): Observable<StripeIntentResponse> {
    return this.http.post<StripeIntentResponse>(`${this.paymentsBase}/stripe/intent`, request);
  }

  getOrder(orderId: string): Observable<OrderDto> {
    return this.http.get<OrderDto>(`${this.ordersBase}/${orderId}`);
  }
}
