import {
  Component,
  inject,
  OnInit,
  ChangeDetectionStrategy,
  signal,
  computed,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { debounceTime, distinctUntilChanged, switchMap, startWith, catchError, take, map } from 'rxjs/operators';
import { of } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { ShippingService } from '../../../core/services/shipping.service';
import { CheckoutStateService } from '../../../core/services/checkout-state.service';
import { AuthService } from '../../../core/services/auth.service';
import { LockerMapComponent } from '../components/locker-map';
import { LockerDto, DeliveryType, ROMANIAN_COUNTIES } from '../../../core/models/shipping.model';

@Component({
  selector: 'app-delivery-step',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DecimalPipe, RouterLink, LockerMapComponent],
  template: `
    <div class="delivery-step">
      <h2 class="step-title">Metoda de livrare</h2>

      <!-- Delivery method cards -->
      <div class="delivery-cards">
        <label class="delivery-card" [class.selected]="deliveryMethod() === 'Easybox'">
          <input
            type="radio"
            name="delivery"
            value="Easybox"
            (change)="selectMethod('Easybox')"
          />
          <div class="card-body">
            <div class="card-title">📦 Easybox Sameday</div>
            <div class="card-price">{{ easyboxCostRon() | number:'1.2-2' }} RON</div>
            <div class="card-desc">Ridicare dintr-un easybox în 24h</div>
          </div>
        </label>

        <label class="delivery-card" [class.selected]="deliveryMethod() === 'Courier'">
          <input
            type="radio"
            name="delivery"
            value="Courier"
            (change)="selectMethod('Courier')"
          />
          <div class="card-body">
            <div class="card-title">🚚 Livrare la ușă</div>
            <div class="card-price">{{ courierCostRon() | number:'1.2-2' }} RON</div>
            <div class="card-desc">Curier la domiciliu în 2–4 zile</div>
          </div>
        </label>
      </div>

      <!-- Easybox locker selection -->
      @if (deliveryMethod() === 'Easybox') {
        <div class="easybox-section">
          <input
            type="text"
            class="city-search"
            placeholder="Caută după oraș (ex: Cluj-Napoca)"
            [formControl]="citySearch"
          />

          @if (lockers().length > 0) {
            <div class="locker-list">
              @for (locker of lockers(); track locker.id) {
                <button
                  type="button"
                  class="locker-item"
                  [class.selected]="selectedLockerId() === locker.id"
                  (click)="selectLocker(locker)"
                >
                  <strong>{{ locker.name }}</strong>
                  <span>{{ locker.address }}</span>
                </button>
              }
            </div>
          }

          @if (citySearch.value && lockers().length === 0) {
            <div class="no-lockers">Niciun easybox găsit pentru acest oraș.</div>
          }

          <app-locker-map
            [lockers]="lockers()"
            [selectedLockerId]="selectedLockerId()"
            (lockerSelected)="selectLocker($event)"
          />

          @if (showLockerError()) {
            <div class="field-error">Selectează un easybox pentru a continua.</div>
          }

          <form [formGroup]="easyboxContactForm" class="address-form" novalidate>
            <div class="form-group">
              <label for="ebName">Nume destinatar</label>
              <input id="ebName" type="text" formControlName="recipientName" />
              @if (easyboxContactForm.get('recipientName')?.touched && easyboxContactForm.get('recipientName')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
            <div class="form-group">
              <label for="ebPhone">Telefon</label>
              <input id="ebPhone" type="tel" formControlName="phone" />
              @if (easyboxContactForm.get('phone')?.touched && easyboxContactForm.get('phone')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
          </form>
        </div>
      }

      <!-- Home delivery address form -->
      @if (deliveryMethod() === 'Courier') {
        <form [formGroup]="addressForm" class="address-form" novalidate>
          <div class="form-row">
            <div class="form-group">
              <label for="street">Strada</label>
              <input id="street" type="text" formControlName="street" />
              @if (touched('street') && addressForm.get('street')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
            <div class="form-group form-group--sm">
              <label for="number">Număr</label>
              <input id="number" type="text" formControlName="number" />
              @if (touched('number') && addressForm.get('number')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
          </div>
          <div class="form-group">
            <label for="block">Bloc / Apart. (opțional)</label>
            <input id="block" type="text" formControlName="block" />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label for="city">Oraș</label>
              <input id="city" type="text" formControlName="city" />
              @if (touched('city') && addressForm.get('city')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
            <div class="form-group">
              <label for="county">Județ</label>
              <select id="county" formControlName="county">
                <option value="">Selectează</option>
                @for (c of counties; track c) {
                  <option [value]="c">{{ c }}</option>
                }
              </select>
              @if (touched('county') && addressForm.get('county')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label for="postalCode">Cod poștal</label>
              <input id="postalCode" type="text" formControlName="postalCode" />
              @if (touched('postalCode') && addressForm.get('postalCode')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
            <div class="form-group">
              <label for="phone">Telefon</label>
              <input id="phone" type="tel" formControlName="phone" />
              @if (touched('phone') && addressForm.get('phone')?.invalid) {
                <span class="field-error">Câmp obligatoriu</span>
              }
            </div>
          </div>
          <div class="form-group">
            <label for="recipientName">Nume destinatar</label>
            <input id="recipientName" type="text" formControlName="recipientName" />
            @if (touched('recipientName') && addressForm.get('recipientName')?.invalid) {
              <span class="field-error">Câmp obligatoriu</span>
            }
          </div>
        </form>
      }

      <div class="step-actions">
        <a routerLink="/cos" class="btn btn--ghost">← Înapoi la coș</a>
        <button
          class="btn btn--primary"
          [disabled]="!canContinue()"
          (click)="continue()"
        >
          Continuă →
        </button>
      </div>
    </div>
  `,
  styles: [`
    .delivery-step { display: flex; flex-direction: column; gap: 1.5rem; }
    .step-title { font-size: 1.4rem; font-weight: 600; margin: 0; }

    .delivery-cards {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .delivery-card {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      border: 2px solid #dee2e6;
      border-radius: 8px;
      padding: 1rem;
      cursor: pointer;
      transition: border-color 0.2s;

      input[type="radio"] { margin-top: 4px; }

      &.selected { border-color: #1a73e8; background: #f0f6ff; }
    }

    .card-title { font-weight: 600; font-size: 1rem; }
    .card-price { color: #1a73e8; font-weight: 700; margin: 0.25rem 0; }
    .card-desc { font-size: 0.85rem; color: #6c757d; }

    .city-search {
      width: 100%;
      padding: 0.6rem 0.8rem;
      border: 1px solid #ced4da;
      border-radius: 6px;
      font-size: 1rem;
    }

    .locker-list {
      max-height: 200px;
      overflow-y: auto;
      border: 1px solid #dee2e6;
      border-radius: 6px;
    }

    .locker-item {
      width: 100%;
      text-align: left;
      padding: 0.6rem 0.8rem;
      background: none;
      border: none;
      border-bottom: 1px solid #f0f0f0;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
      transition: background 0.1s;

      &:hover, &.selected { background: #f0f6ff; }
      span { font-size: 0.85rem; color: #6c757d; }
    }

    .no-lockers { color: #6c757d; font-size: 0.9rem; }

    .address-form { display: flex; flex-direction: column; gap: 0.75rem; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.3rem; }
    .form-group--sm { max-width: 120px; }

    label { font-size: 0.9rem; font-weight: 500; }
    input, select {
      padding: 0.5rem 0.7rem;
      border: 1px solid #ced4da;
      border-radius: 6px;
      font-size: 0.95rem;
    }
    .field-error { color: #dc3545; font-size: 0.8rem; }

    .step-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 0.5rem;
    }


  `],
})
export class DeliveryStep implements OnInit {
  private readonly router = inject(Router);
  private readonly shippingService = inject(ShippingService);
  readonly checkoutState = inject(CheckoutStateService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly counties = ROMANIAN_COUNTIES;

  readonly deliveryMethod = signal<DeliveryType | null>(
    this.checkoutState.snapshot.method,
  );
  readonly easyboxCostRon = signal(20);
  readonly courierCostRon = signal(25);
  readonly lockers = signal<LockerDto[]>([]);
  readonly selectedLockerId = signal<string | null>(this.checkoutState.snapshot.lockerId);
  readonly showLockerError = signal(false);

  readonly citySearch = this.fb.control('');

  readonly addressForm = this.fb.group({
    street: ['', Validators.required],
    number: ['', Validators.required],
    block: [''],
    city: ['', Validators.required],
    county: ['', Validators.required],
    postalCode: ['', Validators.required],
    recipientName: ['', Validators.required],
    phone: ['', Validators.required],
  });

  // Easybox still needs a person + phone for the courier to notify; the locker
  // supplies the address.
  readonly easyboxContactForm = this.fb.group({
    recipientName: ['', Validators.required],
    phone: ['', Validators.required],
  });

  // Form validity is not a signal, so mirror it into one — otherwise the computed
  // below memoizes a stale value and the button never re-enables after typing.
  private readonly easyboxContactValid = toSignal(
    this.easyboxContactForm.statusChanges.pipe(map(s => s === 'VALID')),
    { initialValue: this.easyboxContactForm.valid },
  );
  private readonly addressValid = toSignal(
    this.addressForm.statusChanges.pipe(map(s => s === 'VALID')),
    { initialValue: this.addressForm.valid },
  );

  readonly canContinue = computed(() => {
    const method = this.deliveryMethod();
    if (!method) return false;
    if (method === 'Easybox') return !!this.selectedLockerId() && this.easyboxContactValid();
    return this.addressValid();
  });

  ngOnInit(): void {
    // Load shipping costs
    this.shippingService.getShippingCost('Easybox').subscribe(r => this.easyboxCostRon.set(r.costRon));
    this.shippingService.getShippingCost('Courier').subscribe(r => this.courierCostRon.set(r.costRon));

    // Restore a previously entered address / contact.
    const saved = this.checkoutState.snapshot.shippingAddress;
    if (saved) {
      this.addressForm.patchValue(saved);
      this.easyboxContactForm.patchValue({ recipientName: saved.recipientName, phone: saved.phone });
    }

    this.prefillEasyboxContact();

    // Locker search AND the initial full-list prime run through ONE stream, so
    // switchMap cancels a superseded fetch and a transient error can't tear the
    // subscription down (catchError keeps it alive).
    this.citySearch.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        startWith(this.citySearch.value ?? ''), // immediate prime, bypasses the debounce
        switchMap(city =>
          (city && city.trim().length >= 2
            ? this.shippingService.getLockers(city.trim())
            : this.shippingService.getLockers('')
          ).pipe(catchError(() => of([] as LockerDto[]))),
        ),
      )
      .subscribe(results => this.lockers.set(results));
  }

  private prefillEasyboxContact(): void {
    const c = this.easyboxContactForm;

    // Guest checkout keeps contact under the guestSession key (name/phone).
    try {
      const raw = localStorage.getItem('guestSession');
      if (raw) {
        const g = JSON.parse(raw) as { firstName?: string; lastName?: string; phone?: string };
        const name = [g.firstName, g.lastName].filter(Boolean).join(' ').trim();
        if (!c.value.recipientName && name) c.patchValue({ recipientName: name });
        if (!c.value.phone && g.phone) c.patchValue({ phone: g.phone });
      }
    } catch { /* ignore malformed guestSession */ }

    // Signed-in display name (phone isn't exposed on the client-side user).
    this.auth.currentUser$.pipe(take(1)).subscribe(user => {
      if (user?.displayName && !c.value.recipientName) {
        c.patchValue({ recipientName: user.displayName });
      }
    });
  }

  selectMethod(method: DeliveryType): void {
    this.deliveryMethod.set(method);
    this.showLockerError.set(false);
    const cost = method === 'Easybox' ? this.easyboxCostRon() : this.courierCostRon();
    this.checkoutState.setMethod(method, cost);
  }

  selectLocker(locker: LockerDto): void {
    this.selectedLockerId.set(locker.id);
    this.showLockerError.set(false);
    this.checkoutState.setLocker(locker);
  }

  continue(): void {
    if (!this.canContinue()) {
      if (this.deliveryMethod() === 'Easybox') {
        if (!this.selectedLockerId()) this.showLockerError.set(true);
        this.easyboxContactForm.markAllAsTouched();
      }
      if (this.deliveryMethod() === 'Courier') {
        this.addressForm.markAllAsTouched();
      }
      return;
    }

    if (this.deliveryMethod() === 'Courier') {
      const val = this.addressForm.value;
      this.checkoutState.setShippingAddress({
        street: val['street'] ?? '',
        number: val['number'] ?? '',
        block: val['block'] ?? '',
        city: val['city'] ?? '',
        county: val['county'] ?? '',
        postalCode: val['postalCode'] ?? '',
        recipientName: val['recipientName'] ?? '',
        phone: val['phone'] ?? '',
      });
    } else if (this.deliveryMethod() === 'Easybox') {
      const c = this.easyboxContactForm.value;
      this.checkoutState.setEasyboxContact({
        recipientName: c.recipientName ?? '',
        phone: c.phone ?? '',
      });
    }

    this.router.navigate(['/checkout/recapitulare']);
  }

  touched(field: string): boolean {
    return !!this.addressForm.get(field)?.touched;
  }
}
