import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { DeliveryState, DeliveryType, LockerDto, ShippingAddressForm } from '../models/shipping.model';

const STORAGE_KEY = 'fotoTipar_checkout';

const INITIAL_STATE: DeliveryState = {
  method: null,
  lockerId: null,
  lockerName: null,
  shippingAddress: null,
  shippingCostRon: 0,
};

@Injectable({ providedIn: 'root' })
export class CheckoutStateService {
  private readonly state$$ = new BehaviorSubject<DeliveryState>(this.loadFromStorage());

  readonly deliveryState$ = this.state$$.asObservable();

  get snapshot(): DeliveryState {
    return this.state$$.value;
  }

  setMethod(method: DeliveryType, costRon: number): void {
    this.patch({ method, shippingCostRon: costRon, lockerId: null, lockerName: null, shippingAddress: null });
  }

  setLocker(locker: LockerDto): void {
    this.patch({ lockerId: locker.id, lockerName: locker.name, shippingAddress: null });
  }

  setShippingAddress(address: ShippingAddressForm): void {
    this.patch({ shippingAddress: address, lockerId: null, lockerName: null });
  }

  isDeliveryComplete(): boolean {
    const s = this.state$$.value;
    if (!s.method) return false;
    if (s.method === 'Easybox') return !!s.lockerId;
    return !!s.shippingAddress;
  }

  reset(): void {
    this.state$$.next(INITIAL_STATE);
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch { /* ignore */ }
  }

  private patch(partial: Partial<DeliveryState>): void {
    const next = { ...this.state$$.value, ...partial };
    this.state$$.next(next);
    this.saveToStorage(next);
  }

  private loadFromStorage(): DeliveryState {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (raw) return JSON.parse(raw) as DeliveryState;
    } catch { /* ignore */ }
    return INITIAL_STATE;
  }

  private saveToStorage(state: DeliveryState): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch { /* ignore */ }
  }
}
