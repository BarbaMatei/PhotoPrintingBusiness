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

  // The shipping price arrives after the method is chosen on a restored session, and re-running
  // setMethod there would wipe the locker and address the customer already picked.
  setShippingCost(costRon: number): void {
    if (!this.state$$.value.method) return;
    this.patch({ shippingCostRon: costRon });
  }

  setLocker(locker: LockerDto): void {
    // Keep any Easybox address already entered — only switching delivery method resets it.
    this.patch({ lockerId: locker.id, lockerName: locker.name });
  }

  setShippingAddress(address: ShippingAddressForm): void {
    this.patch({ shippingAddress: address, lockerId: null, lockerName: null });
  }

  // Unlike the courier setter, this keeps the locker: the parcel goes there, the invoice needs the address.
  setEasyboxAddress(address: ShippingAddressForm): void {
    this.patch({ shippingAddress: address });
  }

  isDeliveryComplete(): boolean {
    const s = this.state$$.value;
    if (!s.method) return false;
    if (s.method === 'Easybox') return !!s.lockerId && this.hasFiscalAddress();
    return this.hasFiscalAddress();
  }

  // A session stored before the address was collected restores blank fields, so the object being present is not enough.
  private hasFiscalAddress(): boolean {
    const a = this.state$$.value.shippingAddress;
    if (!a) return false;
    return [a.street, a.number, a.city, a.county, a.postalCode, a.recipientName, a.phone]
      .every(v => !!v && v.trim().length > 0);
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
