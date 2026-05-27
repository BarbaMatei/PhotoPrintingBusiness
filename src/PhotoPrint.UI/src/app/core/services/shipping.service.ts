import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LockerDto, ShippingCostDto, DeliveryType } from '../models/shipping.model';

@Injectable({ providedIn: 'root' })
export class ShippingService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/shipping`;

  getLockers(city: string): Observable<LockerDto[]> {
    const params = new HttpParams().set('city', city);
    return this.http.get<LockerDto[]>(`${this.base}/lockers`, { params });
  }

  getShippingCost(type: DeliveryType): Observable<ShippingCostDto> {
    const params = new HttpParams().set('type', type);
    return this.http.get<ShippingCostDto>(`${this.base}/cost`, { params });
  }
}
