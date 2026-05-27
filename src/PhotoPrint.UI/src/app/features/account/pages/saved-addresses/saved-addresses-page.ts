import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AccountService } from '../../../../core/services/account.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { SavedAddressDto, SavedAddressRequest } from '../../../../core/models/account.model';
import { SpinnerComponent } from '../../../../shared/components/spinner/spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';

const MAX_ADDRESSES = 5;
const PHONE_PATTERN = /^07[0-9]{8}$/;
const POSTAL_PATTERN = /^\d{6}$/;

@Component({
  selector: 'app-saved-addresses-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, NgTemplateOutlet, SpinnerComponent, EmptyStateComponent],
  template: `
    <div class="addresses-page">
      <div class="page-header">
        <h1 class="page-title">Adrese salvate</h1>
        @if (addresses().length < maxAddresses && !showForm()) {
          <button class="btn btn--primary" (click)="openAddForm()">+ Adaugă adresă</button>
        }
      </div>

      @if (loading()) {
        <app-spinner label="Se încarcă adresele..." [showLabel]="true" />
      }

      @if (!loading() && addresses().length === 0 && !showForm()) {
        <app-empty-state
          title="Nu ai nicio adresă salvată."
          actionLabel="Adaugă prima adresă"
          (action)="openAddForm()"
        />
      }

      @if (!loading()) {
        <div class="address-list">
          @for (addr of addresses(); track addr.id) {
            <div class="address-card" [class.address-card--default]="addr.isDefault">
              @if (editingId() !== addr.id) {
                <div class="address-card__header">
                  <span class="address-label">{{ addr.label }}</span>
                  @if (addr.isDefault) {
                    <span class="badge-default">Implicită</span>
                  }
                </div>
                <div class="address-card__body">
                  <p>{{ addr.fullName }}</p>
                  <p>{{ addr.phone }}</p>
                  <p>{{ addr.addressLine }}</p>
                  <p>{{ addr.city }}, {{ addr.county }}, {{ addr.postalCode }}</p>
                </div>
                <div class="address-card__actions">
                  <button class="btn btn--sm btn--ghost" (click)="openEditForm(addr)">Editează</button>
                  <button
                    class="btn btn--sm btn--danger-ghost"
                    [disabled]="deletingId() === addr.id"
                    (click)="deleteAddress(addr.id)"
                  >
                    {{ deletingId() === addr.id ? 'Se șterge...' : 'Șterge' }}
                  </button>
                </div>
              } @else {
                <!-- Inline edit form -->
                <form [formGroup]="addressForm" (ngSubmit)="saveEdit(addr.id)" novalidate class="address-form">
                  <ng-container *ngTemplateOutlet="formFields" />
                  <div class="form-actions">
                    <button type="submit" class="btn btn--primary btn--sm" [disabled]="saving()">
                      {{ saving() ? 'Se salvează...' : 'Salvează' }}
                    </button>
                    <button type="button" class="btn btn--ghost btn--sm" (click)="cancelForm()">Anulează</button>
                  </div>
                </form>
              }
            </div>
          }
        </div>

        <!-- Add new address form -->
        @if (showForm() && editingId() === null) {
          <div class="address-card">
            <h3 class="card-section-title">Adresă nouă</h3>
            <form [formGroup]="addressForm" (ngSubmit)="saveNew()" novalidate class="address-form">
              <ng-container *ngTemplateOutlet="formFields" />
              <div class="form-actions">
                <button type="submit" class="btn btn--primary btn--sm" [disabled]="saving()">
                  {{ saving() ? 'Se salvează...' : 'Adaugă adresa' }}
                </button>
                <button type="button" class="btn btn--ghost btn--sm" (click)="cancelForm()">Anulează</button>
              </div>
            </form>
          </div>
        }
      }

      <!-- Shared form fields template -->
      <ng-template #formFields>
        <div class="form-group">
          <label>Etichetă (ex: Acasă, Birou)</label>
          <input type="text" formControlName="label" class="form-control"
            [class.is-invalid]="fi('label')" />
          @if (fi('label')) { <span class="field-error">Eticheta este obligatorie.</span> }
        </div>
        <div class="form-row">
          <div class="form-group">
            <label>Nume complet</label>
            <input type="text" formControlName="fullName" class="form-control"
              [class.is-invalid]="fi('fullName')" />
            @if (fi('fullName')) { <span class="field-error">Numele complet este obligatoriu.</span> }
          </div>
          <div class="form-group">
            <label>Telefon</label>
            <input type="tel" formControlName="phone" class="form-control"
              [class.is-invalid]="fi('phone')" placeholder="07XXXXXXXX" />
            @if (fi('phone')) { <span class="field-error">Introdu un număr de mobil valid (07XXXXXXXX).</span> }
          </div>
        </div>
        <div class="form-group">
          <label>Adresă</label>
          <input type="text" formControlName="addressLine" class="form-control"
            [class.is-invalid]="fi('addressLine')" placeholder="Str. Exemplu, nr. 1, bl. A, ap. 5" />
          @if (fi('addressLine')) { <span class="field-error">Adresa este obligatorie.</span> }
        </div>
        <div class="form-row form-row--3">
          <div class="form-group">
            <label>Oraș</label>
            <input type="text" formControlName="city" class="form-control"
              [class.is-invalid]="fi('city')" />
            @if (fi('city')) { <span class="field-error">Orașul este obligatoriu.</span> }
          </div>
          <div class="form-group">
            <label>Județ</label>
            <input type="text" formControlName="county" class="form-control"
              [class.is-invalid]="fi('county')" />
            @if (fi('county')) { <span class="field-error">Județul este obligatoriu.</span> }
          </div>
          <div class="form-group">
            <label>Cod poștal</label>
            <input type="text" formControlName="postalCode" class="form-control"
              [class.is-invalid]="fi('postalCode')" placeholder="XXXXXX" />
            @if (fi('postalCode')) { <span class="field-error">Cod poștal invalid (6 cifre).</span> }
          </div>
        </div>
        <div class="form-group form-group--checkbox">
          <label class="checkbox-label">
            <input type="checkbox" formControlName="isDefault" />
            Setează ca adresă implicită
          </label>
        </div>
      </ng-template>
    </div>
  `,
  styles: [`
    .addresses-page {
      padding-bottom: 3rem;
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
      gap: 0.75rem;
    }

    .page-title {
      font-size: 1.5rem;
      font-weight: 700;
      margin: 0;
    }

    // .state-loading and .empty-state removed — replaced by <app-spinner> and <app-empty-state>

    .address-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .address-card {
      background: #fff;
      border: 1px solid #e5e7eb;
      border-radius: 12px;
      padding: 1.25rem 1.5rem;

      &--default {
        border-color: #86efac;
        background: #f0fdf4;
      }
    }

    .address-card__header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.75rem;
    }

    .address-label {
      font-weight: 600;
      font-size: 0.9375rem;
    }

    .badge-default {
      font-size: 0.75rem;
      font-weight: 600;
      background: #16a34a;
      color: #fff;
      padding: 0.125rem 0.5rem;
      border-radius: 999px;
    }

    .address-card__body {
      font-size: 0.9rem;
      color: #374151;
      line-height: 1.6;

      p { margin: 0; }
    }

    .address-card__actions {
      display: flex;
      gap: 0.5rem;
      margin-top: 1rem;
    }

    .card-section-title {
      font-size: 1rem;
      font-weight: 600;
      margin-bottom: 1rem;
    }

    .address-form {
      display: flex;
      flex-direction: column;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.75rem;

      &--3 {
        grid-template-columns: 1fr 1fr 1fr;
      }

      @media (max-width: 480px) {
        grid-template-columns: 1fr;

        &--3 {
          grid-template-columns: 1fr;
        }
      }
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.3rem;
      margin-bottom: 0.875rem;

      label {
        font-size: 0.8125rem;
        font-weight: 500;
        color: #374151;
      }

      &--checkbox {
        flex-direction: row;
        align-items: center;
      }
    }

    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      cursor: pointer;
    }

    .form-control {
      padding: 0.4375rem 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 8px;
      font-size: 0.9375rem;
      outline: none;
      transition: border-color 0.15s, box-shadow 0.15s;

      &:focus {
        border-color: #16a34a;
        box-shadow: 0 0 0 3px rgba(22, 163, 74, 0.15);
      }

      &.is-invalid {
        border-color: #dc2626;
      }
    }

    .field-error {
      font-size: 0.8125rem;
      color: #dc2626;
    }

    .form-actions {
      display: flex;
      gap: 0.5rem;
      margin-top: 0.5rem;
    }

  `],
})
export class SavedAddressesPage implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly maxAddresses = MAX_ADDRESSES;
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly addresses = signal<SavedAddressDto[]>([]);
  readonly showForm = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly addressForm = this.fb.group({
    label: ['', [Validators.required, Validators.maxLength(100)]],
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    phone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    addressLine: ['', [Validators.required, Validators.maxLength(400)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    county: ['', [Validators.required, Validators.maxLength(100)]],
    postalCode: ['', [Validators.required, Validators.pattern(POSTAL_PATTERN)]],
    isDefault: [false],
  });

  ngOnInit(): void {
    this.loadAddresses();
  }

  /** Shorthand: is field invalid and touched? */
  fi(field: string): boolean {
    const ctrl = this.addressForm.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  openAddForm(): void {
    this.addressForm.reset({ isDefault: false });
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEditForm(addr: SavedAddressDto): void {
    this.addressForm.patchValue(addr);
    this.editingId.set(addr.id);
    this.showForm.set(false);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
    this.addressForm.reset({ isDefault: false });
  }

  saveNew(): void {
    if (this.addressForm.invalid || this.saving()) return;
    this.saving.set(true);
    const req = this.formValue();

    this.accountService
      .addAddress(req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (addr) => {
          this.applyDefaultFlag(addr);
          this.addresses.update(list => [...list, addr]);
          this.cancelForm();
          this.saving.set(false);
          this.toast.show('Adresa a fost adăugată.', 'success');
          this.cdr.markForCheck();
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          if (err.status === 409) {
            this.toast.show('Poți salva maximum 5 adrese.', 'warning');
          } else {
            this.toast.show('Eroare la adăugarea adresei.', 'error');
          }
          this.cdr.markForCheck();
        },
      });
  }

  saveEdit(id: string): void {
    if (this.addressForm.invalid || this.saving()) return;
    this.saving.set(true);
    const req = this.formValue();

    this.accountService
      .updateAddress(id, req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.applyDefaultFlag(updated);
          this.addresses.update(list =>
            list.map(a => a.id === id ? updated : req.isDefault ? { ...a, isDefault: false } : a)
          );
          this.cancelForm();
          this.saving.set(false);
          this.toast.show('Adresa a fost actualizată.', 'success');
          this.cdr.markForCheck();
        },
        error: () => {
          this.saving.set(false);
          this.toast.show('Eroare la actualizarea adresei.', 'error');
          this.cdr.markForCheck();
        },
      });
  }

  deleteAddress(id: string): void {
    if (this.deletingId()) return;
    this.deletingId.set(id);

    this.accountService
      .deleteAddress(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addresses.update(list => list.filter(a => a.id !== id));
          this.deletingId.set(null);
          this.toast.show('Adresa a fost ștearsă.', 'success');
          this.cdr.markForCheck();
        },
        error: () => {
          this.deletingId.set(null);
          this.toast.show('Eroare la ștergerea adresei.', 'error');
          this.cdr.markForCheck();
        },
      });
  }

  private loadAddresses(): void {
    this.accountService
      .getAddresses()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          this.addresses.set(list);
          this.loading.set(false);
          this.cdr.markForCheck();
        },
        error: () => {
          this.loading.set(false);
          this.toast.show('Nu s-au putut încărca adresele.', 'error');
          this.cdr.markForCheck();
        },
      });
  }

  private formValue(): SavedAddressRequest {
    const v = this.addressForm.getRawValue();
    return {
      label: v.label!,
      fullName: v.fullName!,
      phone: v.phone!,
      addressLine: v.addressLine!,
      city: v.city!,
      county: v.county!,
      postalCode: v.postalCode!,
      isDefault: v.isDefault ?? false,
    };
  }

  /** When saving a default address, clear isDefault on all others in the signal. */
  private applyDefaultFlag(saved: SavedAddressDto): void {
    if (saved.isDefault) {
      this.addresses.update(list => list.map(a => ({ ...a, isDefault: false })));
    }
  }
}
