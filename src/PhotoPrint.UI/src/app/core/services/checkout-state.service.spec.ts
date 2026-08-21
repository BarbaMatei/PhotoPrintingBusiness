import { TestBed } from '@angular/core/testing';
import { CheckoutStateService } from './checkout-state.service';
import { LockerDto, ShippingAddressForm } from '../models/shipping.model';

const FISCAL_ADDRESS: ShippingAddressForm = {
  street: 'Str. Buyer',
  number: '10',
  block: '',
  city: 'Cluj-Napoca',
  county: 'Cluj',
  postalCode: '400100',
  recipientName: 'Ana Pop',
  phone: '0712345678',
};

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

  it('isDeliveryComplete is false for Easybox with a locker but no address at all', () => {
    // Otherwise the stepper unlocks payment before the contact exists → 400 server-side.
    service.setMethod('Easybox', 20);
    service.setLocker(LOCKER);
    expect(service.isDeliveryComplete()).toBe(false);
  });

  it('isDeliveryComplete is true for Easybox once locker + fiscal address are set', () => {
    service.setMethod('Easybox', 20);
    service.setLocker(LOCKER);
    service.setEasyboxAddress(FISCAL_ADDRESS);
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

  it('setLocker preserves an already-entered Easybox address', () => {
    service.setMethod('Easybox', 20);
    service.setEasyboxAddress(FISCAL_ADDRESS);
    service.setLocker(LOCKER);
    expect(service.snapshot.lockerId).toBe(LOCKER.id);
    expect(service.snapshot.shippingAddress?.recipientName).toBe('Ana Pop');
    expect(service.snapshot.shippingAddress?.postalCode).toBe('400100');
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

  it('isDeliveryComplete is false for Easybox with a locker and contact but no fiscal address', () => {
    service.setMethod('Easybox', 20);
    service.setLocker(LOCKER);
    service.setEasyboxAddress({ ...FISCAL_ADDRESS, street: '', city: '', postalCode: '' });

    expect(service.snapshot.shippingAddress?.recipientName).toBe('Ana Pop');
    expect(service.isDeliveryComplete()).toBe(false);
  });

  it('a session stored before the fiscal address was collected does not unlock payment', () => {
    sessionStorage.setItem(
      'fotoTipar_checkout',
      JSON.stringify({
        method: 'Easybox',
        lockerId: 'locker-1',
        lockerName: 'Easybox Mall',
        shippingAddress: {
          street: '',
          number: '',
          block: '',
          city: '',
          county: '',
          postalCode: '',
          recipientName: 'Ana Pop',
          phone: '0712345678',
        },
        shippingCostRon: 20,
      }),
    );
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const restored = TestBed.inject(CheckoutStateService);

    expect(restored.snapshot.lockerId).toBe('locker-1');
    expect(restored.isDeliveryComplete()).toBe(false);
  });

});
