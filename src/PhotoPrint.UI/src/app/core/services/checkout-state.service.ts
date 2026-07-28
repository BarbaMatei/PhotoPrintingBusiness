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
    // Keep any Easybox contact already entered — only switching delivery method resets it.
    this.patch({ lockerId: locker.id, lockerName: locker.name });
  }

  setShippingAddress(address: ShippingAddressForm): void {
    this.patch({ shippingAddress: address, lockerId: null, lockerName: null });
  }

  /** Easybox recipient contact: the locker supplies the address, so the address
   *  fields are blank, but the locker selection is preserved. */
  setEasyboxContact(contact: { recipientName: string; phone: string }): void {
    const address: ShippingAddressForm = {
      street: '', number: '', block: '', city: '', county: '', postalCode: '',
      recipientName: contact.recipientName, phone: contact.phone,
    };
    this.patch({ shippingAddress: address });
  }

  isDeliveryComplete(): boolean {
    const s = this.state$$.value;
    if (!s.method) return false;
    // Easybox needs the locker AND the recipient contact (set by setEasyboxContact
    // on Continue) — otherwise the stepper would unlock payment before the contact
    // exists and the order would 400 server-side.
    if (s.method === 'Easybox') {
      return !!s.lockerId && !!s.shippingAddress?.recipientName && !!s.shippingAddress?.phone;
    }
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
