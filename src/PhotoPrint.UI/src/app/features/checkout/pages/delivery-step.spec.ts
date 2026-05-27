import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { DeliveryStep } from './delivery-step';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { environment } from '../../../../environments/environment';

describe('DeliveryStep', () => {
  let http: HttpTestingController;
  const BASE = `${environment.apiUrl}/shipping`;

  function createFixture() {
    const fixture = TestBed.createComponent(DeliveryStep);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    sessionStorage.clear();
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
  });

  function flushShippingCosts() {
    http.expectOne(`${BASE}/cost?type=Easybox`).flush({ costRon: 20 });
    http.expectOne(`${BASE}/cost?type=Courier`).flush({ costRon: 25 });
  }

  it('renders two delivery option cards', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();
    const cards = fixture.debugElement.queryAll(By.css('.delivery-card'));
    expect(cards.length).toBe(2);
  });

  it('Continue button is disabled when no method selected', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('Selecting Easybox shows locker map section', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.selectMethod('Easybox');
    fixture.detectChanges();

    const section = fixture.debugElement.query(By.css('.easybox-section'));
    expect(section).not.toBeNull();
  });

  it('Selecting Easybox leaves Continue disabled until locker chosen', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();

    fixture.componentInstance.selectMethod('Easybox');
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('Selecting Easybox + locker enables Continue', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    comp.selectMethod('Easybox');
    comp.selectLocker({
      id: 'l1', samedayId: 'SD1', name: 'Box', address: 'Str 1', city: 'Cluj', lat: 46, lng: 23,
    });
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(false);
  });

  it('Selecting Courier shows address form', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();

    fixture.componentInstance.selectMethod('Courier');
    fixture.detectChanges();

    const form = fixture.debugElement.query(By.css('.address-form'));
    expect(form).not.toBeNull();
  });

  it('Courier Continue is disabled when form is invalid', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();

    fixture.componentInstance.selectMethod('Courier');
    fixture.detectChanges();

    const btn = fixture.debugElement.query(By.css('.btn--primary')).nativeElement as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('city search debounces API calls', async () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();

    fixture.componentInstance.selectMethod('Easybox');
    fixture.detectChanges();

    // Type quickly then wait for debounce
    fixture.componentInstance.citySearch.setValue('Clu');
    await new Promise(r => setTimeout(r, 350)); // after 300ms debounce
    const req = http.expectOne(`${BASE}/lockers?city=Clu`);
    req.flush([]);
  });

  it('selectLocker updates CheckoutStateService', () => {
    const fixture = createFixture();
    flushShippingCosts();
    fixture.detectChanges();

    const stateService = TestBed.inject(CheckoutStateService);
    fixture.componentInstance.selectMethod('Easybox');
    fixture.componentInstance.selectLocker({
      id: 'lck', samedayId: 'SD1', name: 'Box A', address: 'Str 1', city: 'Timișoara', lat: 45, lng: 21,
    });

    expect(stateService.snapshot.lockerId).toBe('lck');
    expect(stateService.snapshot.lockerName).toBe('Box A');
  });
});
