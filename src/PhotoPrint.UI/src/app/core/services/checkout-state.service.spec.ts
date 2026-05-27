import { TestBed } from '@angular/core/testing';
import { CheckoutStateService } from './checkout-state.service';
import { LockerDto } from '../models/shipping.model';

const LOCKER: LockerDto = {
  id: 'locker-1',
  samedayId: 'SD-001',
  name: 'Easybox Mall',
  address: 'Str. Test 1',
  city: 'Cluj-Napoca',
  lat: 46.77,
  lng: 23.59,
};

describe('CheckoutStateService', () => {
  let service: CheckoutStateService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(CheckoutStateService);
  });

  afterEach(() => sessionStorage.clear());

  it('starts with null method', () => {
    expect(service.snapshot.method).toBeNull();
  });

  it('isDeliveryComplete returns false when no method selected', () => {
    expect(service.isDeliveryComplete()).toBe(false);
  });

  it('setMethod updates method and shippingCostRon', () => {
    service.setMethod('Easybox', 20);
    expect(service.snapshot.method).toBe('Easybox');
    expect(service.snapshot.shippingCostRon).toBe(20);
  });

  it('isDeliveryComplete is false for Easybox without locker', () => {
    service.setMethod('Easybox', 20);
    expect(service.isDeliveryComplete()).toBe(false);
  });

  it('isDeliveryComplete is true for Easybox after locker selected', () => {
    service.setMethod('Easybox', 20);
    service.setLocker(LOCKER);
    expect(service.isDeliveryComplete()).toBe(true);
  });

  it('setLocker stores lockerId and lockerName', () => {
    service.setMethod('Easybox', 20);
    service.setLocker(LOCKER);
    expect(service.snapshot.lockerId).toBe('locker-1');
    expect(service.snapshot.lockerName).toBe('Easybox Mall');
  });

  it('isDeliveryComplete is false for Courier without address', () => {
    service.setMethod('Courier', 25);
    expect(service.isDeliveryComplete()).toBe(false);
  });

  it('isDeliveryComplete is true for Courier after address set', () => {
    service.setMethod('Courier', 25);
    service.setShippingAddress({
      street: 'Str. Test',
      number: '1',
      block: '',
      city: 'București',
      county: 'Ilfov',
      postalCode: '011111',
      recipientName: 'Ion Popescu',
      phone: '0700000000',
    });
    expect(service.isDeliveryComplete()).toBe(true);
  });

  it('reset clears state', () => {
    service.setMethod('Easybox', 20);
    service.setLocker(LOCKER);
    service.reset();
    expect(service.snapshot.method).toBeNull();
    expect(service.snapshot.lockerId).toBeNull();
  });

  it('persists state to sessionStorage', () => {
    service.setMethod('Courier', 25);
    const raw = sessionStorage.getItem('fotoTipar_checkout');
    expect(raw).not.toBeNull();
    const parsed = JSON.parse(raw!);
    expect(parsed.method).toBe('Courier');
  });
});
