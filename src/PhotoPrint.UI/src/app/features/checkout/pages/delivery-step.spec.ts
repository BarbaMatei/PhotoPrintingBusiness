import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { DeliveryStep } from './delivery-step';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { environment } from '../../../../environments/environment';

describe('DeliveryStep', () => {
  let http: HttpTestingController;
  const BASE = `${environment.apiUrl}/shipping`;

  const locker = (id: string, name = 'Box') => ({
    id, samedayId: 'SD' + id, name, address: 'Str 1', city: 'Cluj', lat: 46, lng: 23,
  });

  function createFixture() {
    const fixture = TestBed.createComponent(DeliveryStep);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [DeliveryStep],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
    localStorage.clear();
  });

  // Init fires only the two cost calls now — the locker list is primed lazily, when Easybox is
  // selected (a courier-only user never triggers a locker fetch).
  function flushCosts() {
    http.expectOne(`${BASE}/cost?type=Easybox`).flush({ costRon: 20 });
    http.expectOne(`${BASE}/cost?type=Courier`).flush({ costRon: 25 });
  }

  // Selecting Easybox primes the full locker list through the search stream.
  function selectEasybox(fixture: ReturnType<typeof createFixture>, prime: unknown[] = []) {
    fixture.componentInstance.selectMethod('Easybox');
    fixture.detectChanges();
    http.expectOne(`${BASE}/lockers?city=`).flush(prime);
  }

  it('renders two delivery option cards', () => {
    const fixture = createFixture();
    flushCosts();
    fixture.detectChanges();
    const cards = fixture.debugElement.queryAll(By.css('.delivery-card'));
    expect(cards.length).toBe(2);
  });

  it('Continue button is disabled when no method selected', () => {
    const fixture = createFixture();
    flushCosts();
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('does not fetch lockers on init for a courier-only user', () => {
    const fixture = createFixture();
    flushCosts();
    fixture.componentInstance.selectMethod('Courier');
    fixture.detectChanges();
    // No lockers request outstanding — http.verify() in afterEach would fail if one fired.
    expect(fixture.componentInstance.lockers().length).toBe(0);
  });

  it('Selecting Easybox shows locker map section and primes the list', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture, [locker('l1')]);

    const section = fixture.debugElement.query(By.css('.easybox-section'));
    expect(section).not.toBeNull();
    expect(fixture.componentInstance.lockers().length).toBe(1);
  });

  it('Selecting Easybox leaves Continue disabled until locker chosen', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('Selecting Easybox + locker but no fiscal address keeps Continue disabled', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    fixture.componentInstance.selectLocker(locker('l1'));
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('typing the Easybox address after selecting a locker re-enables Continue', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    const comp = fixture.componentInstance;
    comp.selectLocker(locker('l1'));
    fixture.detectChanges();
    let btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);

    // canContinue must react to form validity — a memoized computed reading form.valid (not a
    // signal) would stay disabled here.
    comp.addressForm.setValue(FISCAL_ADDRESS);
    fixture.detectChanges();
    btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(false);
  });

  it('a digit-poor Easybox phone keeps Continue disabled (mirrors the server rule)', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    const comp = fixture.componentInstance;
    comp.selectLocker(locker('l1'));
    comp.addressForm.setValue({ ...FISCAL_ADDRESS, phone: '1-2-3-4' });
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('Easybox continue captures the recipient contact into checkout state', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    const state = TestBed.inject(CheckoutStateService);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const comp = fixture.componentInstance;
    comp.selectLocker(locker('l1'));
    comp.addressForm.setValue(FISCAL_ADDRESS);
    comp.continue();

    expect(state.snapshot.lockerId).toBe('l1');
    expect(state.snapshot.shippingAddress?.recipientName).toBe('Ana Pop');
    expect(state.snapshot.shippingAddress?.phone).toBe('0712345678');
  });

  it('switching Easybox → Courier → Easybox clears the stale locker selection', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture, [locker('l1')]);

    const comp = fixture.componentInstance;
    comp.selectLocker(locker('l1'));
    expect(comp.selectedLockerId()).toBe('l1');

    comp.selectMethod('Courier');
    fixture.detectChanges();
    // Back to Easybox re-primes the list; the previously chosen locker must not linger.
    selectEasybox(fixture, [locker('l1')]);
    expect(comp.selectedLockerId()).toBeNull();
  });

  it('Selecting Courier shows address form', () => {
    const fixture = createFixture();
    flushCosts();
    fixture.componentInstance.selectMethod('Courier');
    fixture.detectChanges();

    const form = fixture.debugElement.query(By.css('.address-form'));
    expect(form).not.toBeNull();
  });

  it('city search debounces API calls', async () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    fixture.componentInstance.citySearch.setValue('Clu');
    await new Promise(r => setTimeout(r, 350));
    http.expectOne(`${BASE}/lockers?city=Clu`).flush([]);
  });

  it('clearing the city search restores the full locker list', async () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    fixture.componentInstance.citySearch.setValue('Clu');
    await new Promise(r => setTimeout(r, 350));
    http.expectOne(`${BASE}/lockers?city=Clu`).flush([locker('l1', 'Box Cluj')]);
    expect(fixture.componentInstance.lockers().length).toBe(1);

    fixture.componentInstance.citySearch.setValue('');
    await new Promise(r => setTimeout(r, 350));
    http.expectOne(`${BASE}/lockers?city=`).flush([locker('l1'), locker('l2')]);
    expect(fixture.componentInstance.lockers().length).toBe(2);
  });

  it('a failed locker search surfaces a distinct error, then recovers', async () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);
    const comp = fixture.componentInstance;

    comp.citySearch.setValue('Clu');
    await new Promise(r => setTimeout(r, 350));
    http.expectOne(`${BASE}/lockers?city=Clu`).flush('boom', { status: 500, statusText: 'Server Error' });
    expect(comp.lockerSearchError()).toBe(true); // not shown as "no lockers in this city"

    comp.citySearch.setValue('Bucur');
    await new Promise(r => setTimeout(r, 350));
    http.expectOne(`${BASE}/lockers?city=Bucur`).flush([locker('l2', 'Box B')]);
    expect(comp.lockerSearchError()).toBe(false); // cleared as the new fetch starts
    expect(comp.lockers().length).toBe(1);
  });

  it('selectLocker updates CheckoutStateService', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    const stateService = TestBed.inject(CheckoutStateService);
    fixture.componentInstance.selectLocker(locker('lck', 'Box A'));

    expect(stateService.snapshot.lockerId).toBe('lck');
    expect(stateService.snapshot.lockerName).toBe('Box A');
  });

  // ── Guest-session prefill (guest-state parsing) ─────────

  it('prefills the Easybox contact from a stored guest session', () => {
    localStorage.setItem('guestSession', JSON.stringify({
      firstName: 'Ana', lastName: 'Pop', phone: '0712345678', guestToken: 't',
    }));
    const fixture = createFixture();
    flushCosts();

    const c = fixture.componentInstance.addressForm;
    expect(c.value.recipientName).toBe('Ana Pop');
    expect(c.value.phone).toBe('0712345678');
  });

  it('a malformed guest session does not throw and leaves the contact empty', () => {
    localStorage.setItem('guestSession', '{ not json');
    expect(() => {
      const fixture = createFixture();
      flushCosts();
      expect(fixture.componentInstance.addressForm.value.recipientName).toBeFalsy();
    }).not.toThrow();
  });

  const FISCAL_ADDRESS = {
    street: 'Str. Buyer',
    number: '10',
    block: '',
    city: 'Cluj-Napoca',
    county: 'Cluj',
    postalCode: '400100',
    recipientName: 'Ana Pop',
    phone: '0712345678',
  };

  it('the Easybox branch asks for the same fiscal address fields as home delivery', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    expect(fixture.debugElement.query(By.css('#street'))).not.toBeNull();
    expect(fixture.debugElement.query(By.css('#city'))).not.toBeNull();
    expect(fixture.debugElement.query(By.css('#postalCode'))).not.toBeNull();
  });

  it('an Easybox order with a locker and a full fiscal address enables Continue and stores both', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    const state = TestBed.inject(CheckoutStateService);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const comp = fixture.componentInstance;
    comp.selectLocker(locker('l1'));
    comp.addressForm.setValue(FISCAL_ADDRESS);
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(false);

    comp.continue();

    expect(state.snapshot.lockerId).toBe('l1');
    expect(state.snapshot.shippingAddress?.street).toBe('Str. Buyer');
    expect(state.snapshot.shippingAddress?.city).toBe('Cluj-Napoca');
    expect(state.snapshot.shippingAddress?.postalCode).toBe('400100');
    expect(state.snapshot.shippingAddress?.recipientName).toBe('Ana Pop');
  });

  it('an Easybox locker with a contact but a blank fiscal address keeps Continue disabled', () => {
    const fixture = createFixture();
    flushCosts();
    selectEasybox(fixture);

    const comp = fixture.componentInstance;
    comp.selectLocker(locker('l1'));
    comp.addressForm.patchValue({ recipientName: 'Ana Pop', phone: '0712345678' });
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('prefills a signed-in customer’s default saved address, leaving the street line to them', () => {
    // atob is what AuthService uses to read the token, so build the payload the same way.
    const payload = btoa(JSON.stringify({
      sub: 'u1', email: 'ana@example.ro', name: 'Ana Pop',
      exp: Math.floor(Date.now() / 1000) + 3600,
    }));
    sessionStorage.setItem('access_token', `header.${payload}.signature`);

    const fixture = createFixture();
    flushCosts();
    http.expectOne(`${environment.apiUrl}/account/addresses`).flush([
      {
        id: 'a1', label: 'Acasă', fullName: 'Ana Pop', phone: '0712345678',
        addressLine: 'Str. Salvată 4', city: 'Cluj-Napoca', county: 'Cluj',
        postalCode: '400100', isDefault: true,
      },
    ]);
    fixture.detectChanges();

    const form = fixture.componentInstance.addressForm;
    expect(form.value.city).toBe('Cluj-Napoca');
    expect(form.value.county).toBe('Cluj');
    expect(form.value.postalCode).toBe('400100');
    expect(form.value.street).toBeFalsy();
    expect(form.value.number).toBeFalsy();
  });

  it('does not fetch saved addresses for a guest', () => {
    const fixture = createFixture();
    flushCosts();
    fixture.detectChanges();
    // http.verify() in afterEach fails if an /account/addresses request was made.
    expect(fixture.componentInstance.addressForm.value.city).toBeFalsy();
  });
});
