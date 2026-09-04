import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './api/base-api.service';
import { LockerDto, ShippingCostDto, DeliveryType } from '../models/shipping.model';

@Injectable({ providedIn: 'root' })
export class ShippingService {
  private readonly api = inject(BaseApiService);
  private readonly base = '/shipping';

  getLockers(city: string): Observable<LockerDto[]> {
    return this.api.get<LockerDto[]>(`${this.base}/lockers`, { params: { city } });
  }

  getShippingCost(type: DeliveryType): Observable<ShippingCostDto> {
    return this.api.get<ShippingCostDto>(`${this.base}/cost`, { params: { type } });
  }
}
