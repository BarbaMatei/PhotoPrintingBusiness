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
import { AddressForm } from './components/address-form/address-form';
import { AddressListItem } from './components/address-list-item/address-list-item';

const MAX_ADDRESSES = 5;
const PHONE_PATTERN = /^07[0-9]{8}$/;
const POSTAL_PATTERN = /^\d{6}$/;

@Component({
  selector: 'app-saved-addresses-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    SpinnerComponent,
    EmptyStateComponent,
    AddressForm,
    AddressListItem,
  ],
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
                <app-address-list-item
                  [address]="addr"
                  [deleting]="deletingId() === addr.id"
                  (edit)="openEditForm($event)"
                  (remove)="deleteAddress($event)"
                />
              } @else {
                <form
                  [formGroup]="addressForm"
                  (ngSubmit)="saveEdit(addr.id)"
                  novalidate
                  class="address-form"
                >
                  <app-address-form />
                  <div class="form-actions">
                    <button type="submit" class="btn btn--primary btn--sm" [disabled]="saving()">
                      {{ saving() ? 'Se salvează...' : 'Salvează' }}
                    </button>
                    <button type="button" class="btn btn--ghost btn--sm" (click)="cancelForm()">
                      Anulează
                    </button>
                  </div>
                </form>
              }
            </div>
          }
        </div>

        @if (showForm() && editingId() === null) {
          <div class="address-card">
            <h3 class="card-section-title">Adresă nouă</h3>
            <form [formGroup]="addressForm" (ngSubmit)="saveNew()" novalidate class="address-form">
              <app-address-form />
              <div class="form-actions">
                <button type="submit" class="btn btn--primary btn--sm" [disabled]="saving()">
                  {{ saving() ? 'Se salvează...' : 'Adaugă adresa' }}
                </button>
                <button type="button" class="btn btn--ghost btn--sm" (click)="cancelForm()">
                  Anulează
                </button>
              </div>
            </form>
          </div>
        }
      }
    </div>
  `,
  styles: [
    `
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

      .card-section-title {
        font-size: 1rem;
        font-weight: 600;
        margin-bottom: 1rem;
      }

      .address-form {
        display: flex;
        flex-direction: column;
      }

      .form-actions {
        display: flex;
        gap: 0.5rem;
        margin-top: 0.5rem;
      }
    `,
  ],
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
          this.addresses.update((list) => [...list, addr]);
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
          this.addresses.update((list) =>
            list.map((a) =>
              a.id === id ? updated : req.isDefault ? { ...a, isDefault: false } : a,
            ),
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
          this.addresses.update((list) => list.filter((a) => a.id !== id));
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
      this.addresses.update((list) => list.map((a) => ({ ...a, isDefault: false })));
    }
  }
}
