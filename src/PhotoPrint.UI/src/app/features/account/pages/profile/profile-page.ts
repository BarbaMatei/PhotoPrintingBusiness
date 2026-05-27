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
import { Router } from '@angular/router';
import { AccountService } from '../../../../core/services/account.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { passwordStrengthValidator } from '../../../../shared/validators/password-strength.validator';
import { AccountDto } from '../../../../core/models/account.model';
import { SpinnerComponent } from '../../../../shared/components/spinner/spinner.component';
import { PasswordChecklistComponent } from '../../../../shared/components/password-checklist/password-checklist.component';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SpinnerComponent, PasswordChecklistComponent],
  template: `
    <div class="profile-page">
      <h1 class="page-title">Profilul meu</h1>

      @if (loading()) {
        <app-spinner label="Se încarcă..." [showLabel]="true" />
      }

      @if (!loading() && account()) {
        <!-- Profile form -->
        <section class="card">
          <h2 class="card__title">Date personale</h2>
          <form [formGroup]="profileForm" (ngSubmit)="saveProfile()" novalidate>
            <div class="form-row">
              <div class="form-group">
                <label for="firstName">Prenume</label>
                <input
                  id="firstName"
                  type="text"
                  formControlName="firstName"
                  class="form-control"
                  [class.is-invalid]="isInvalid('profileForm', 'firstName')"
                />
                @if (isInvalid('profileForm', 'firstName')) {
                  <span class="field-error">Prenumele este obligatoriu.</span>
                }
              </div>
              <div class="form-group">
                <label for="lastName">Nume de familie</label>
                <input
                  id="lastName"
                  type="text"
                  formControlName="lastName"
                  class="form-control"
                  [class.is-invalid]="isInvalid('profileForm', 'lastName')"
                />
                @if (isInvalid('profileForm', 'lastName')) {
                  <span class="field-error">Numele este obligatoriu.</span>
                }
              </div>
            </div>

            <div class="form-group">
              <label for="email">Email</label>
              <input id="email" type="email" [value]="account()!.email" class="form-control" disabled />
            </div>

            <div class="form-group">
              <label for="phone">Telefon (opțional)</label>
              <input
                id="phone"
                type="tel"
                formControlName="phone"
                class="form-control"
                [class.is-invalid]="isInvalid('profileForm', 'phone')"
                placeholder="07XXXXXXXX"
              />
              @if (isInvalid('profileForm', 'phone')) {
                <span class="field-error">Introdu un număr de mobil valid (07XXXXXXXX).</span>
              }
            </div>

            <button
              type="submit"
              class="btn btn--primary"
              [disabled]="profileSaving()"
            >
              {{ profileSaving() ? 'Se salvează...' : 'Salvează modificările' }}
            </button>
          </form>
        </section>

        <!-- Change password -->
        @if (account()!.hasPassword) {
          <section class="card">
            <h2 class="card__title">Schimbă parola</h2>
            <form [formGroup]="passwordForm" (ngSubmit)="changePassword()" novalidate>
              <div class="form-group">
                <label for="currentPassword">Parola actuală</label>
                <input
                  id="currentPassword"
                  type="password"
                  formControlName="currentPassword"
                  class="form-control"
                  [class.is-invalid]="isInvalid('passwordForm', 'currentPassword')"
                />
                @if (isInvalid('passwordForm', 'currentPassword')) {
                  <span class="field-error">Parola actuală este obligatorie.</span>
                }
              </div>

              <div class="form-group">
                <label for="newPassword">Parola nouă</label>
                <input
                  id="newPassword"
                  type="password"
                  formControlName="newPassword"
                  class="form-control"
                  [class.is-invalid]="isInvalid('passwordForm', 'newPassword')"
                />
                @if (isInvalid('passwordForm', 'newPassword')) {
                  <span class="field-error">Parola nouă este obligatorie.</span>
                }
                <app-password-checklist [password]="passwordForm.get('newPassword')?.value ?? ''" />
              </div>

              <div class="form-group">
                <label for="confirmNewPassword">Confirmă parola nouă</label>
                <input
                  id="confirmNewPassword"
                  type="password"
                  formControlName="confirmNewPassword"
                  class="form-control"
                  [class.is-invalid]="isInvalid('passwordForm', 'confirmNewPassword') || passwordForm.errors?.['mismatch']"
                />
                @if (passwordForm.errors?.['mismatch'] && passwordForm.get('confirmNewPassword')?.touched) {
                  <span class="field-error">Parolele nu coincid.</span>
                }
              </div>

              @if (passwordError()) {
                <p class="form-error">{{ passwordError() }}</p>
              }

              <button
                type="submit"
                class="btn btn--primary"
                [disabled]="passwordSaving()"
              >
                {{ passwordSaving() ? 'Se salvează...' : 'Schimbă parola' }}
              </button>
            </form>
          </section>
        }

        <!-- Delete account -->
        <section class="card card--danger">
          <h2 class="card__title card__title--danger">Ștergere cont</h2>
          @if (account()!.deletionRequested) {
            <p class="danger-notice">
              Cererea ta de ștergere a contului a fost înregistrată. Contul va fi șters în termen de 30 de zile.
            </p>
          } @else {
            <p class="danger-notice">
              Dacă ștergi contul, toate datele tale vor fi eliminate definitiv după 30 de zile.
              Comenzile în curs nu vor fi afectate.
            </p>
            @if (!showDeleteConfirm()) {
              <button class="btn btn--danger" (click)="showDeleteConfirm.set(true)">
                Solicită ștergerea contului
              </button>
            } @else {
              <p class="danger-confirm-text">
                Ești sigur că vrei să soliciți ștergerea contului? Această acțiune nu poate fi anulată.
              </p>
              <div class="btn-group">
                <button
                  class="btn btn--danger"
                  [disabled]="deletionSaving()"
                  (click)="requestDeletion()"
                >
                  {{ deletionSaving() ? 'Se procesează...' : 'Confirm ștergerea' }}
                </button>
                <button class="btn btn--ghost" (click)="showDeleteConfirm.set(false)">Anulează</button>
              </div>
            }
          }
        </section>
      }
    </div>
  `,
  styles: [`
    .profile-page {
      padding-bottom: 3rem;
    }

    .page-title {
      font-size: 1.5rem;
      font-weight: 700;
      margin-bottom: 1.5rem;
    }

    // .state-loading removed — replaced by <app-spinner>

    .card {
      background: #fff;
      border: 1px solid #e5e7eb;
      border-radius: 12px;
      padding: 1.5rem;
      margin-bottom: 1.5rem;

      &--danger {
        border-color: #fca5a5;
        background: #fff5f5;
      }
    }

    .card__title {
      font-size: 1.0625rem;
      font-weight: 600;
      margin-bottom: 1.25rem;
      color: #111827;

      &--danger {
        color: #dc2626;
      }
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;

      @media (max-width: 480px) {
        grid-template-columns: 1fr;
      }
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
      margin-bottom: 1rem;

      label {
        font-size: 0.875rem;
        font-weight: 500;
        color: #374151;
      }
    }

    .form-control {
      padding: 0.5rem 0.75rem;
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

      &:disabled {
        background: #f9fafb;
        color: #6b7280;
        cursor: not-allowed;
      }
    }

    .field-error {
      font-size: 0.8125rem;
      color: #dc2626;
    }

    .field-error-list {
      font-size: 0.8125rem;
      color: #dc2626;
      margin: 0.25rem 0 0 1rem;
      padding: 0;
    }

    .form-error {
      font-size: 0.875rem;
      color: #dc2626;
      margin-bottom: 0.75rem;
    }

    .btn-group {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .danger-notice {
      font-size: 0.9375rem;
      color: #374151;
      margin-bottom: 1rem;
    }

    .danger-confirm-text {
      font-size: 0.9375rem;
      color: #dc2626;
      font-weight: 500;
      margin-bottom: 1rem;
    }
  `],
})
export class ProfilePage implements OnInit {
  private readonly account$ = inject(AccountService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly profileSaving = signal(false);
  readonly passwordSaving = signal(false);
  readonly deletionSaving = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly account = signal<AccountDto | null>(null);
  readonly passwordError = signal<string | null>(null);

  readonly profileForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: [
      '',
      [Validators.pattern(/^07[0-9]{8}$/)],
    ],
  });

  readonly passwordForm = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, passwordStrengthValidator]],
      confirmNewPassword: ['', Validators.required],
    },
    {
      validators: [
        (group) => {
          const pw = group.get('newPassword')?.value ?? '';
          const cpw = group.get('confirmNewPassword')?.value ?? '';
          return pw !== cpw ? { mismatch: true } : null;
        },
      ],
    }
  );

  ngOnInit(): void {
    this.account$
      .getAccount()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dto) => {
          this.account.set(dto);
          this.profileForm.patchValue({
            firstName: dto.firstName,
            lastName: dto.lastName,
            phone: dto.phone ?? '',
          });
          this.loading.set(false);
          this.cdr.markForCheck();
        },
        error: () => {
          this.loading.set(false);
          this.toast.show('Nu s-au putut încărca datele contului.', 'error');
          this.cdr.markForCheck();
        },
      });
  }

  isInvalid(form: 'profileForm' | 'passwordForm', field: string): boolean {
    const ctrl = form === 'profileForm'
      ? this.profileForm.get(field)
      : this.passwordForm.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  saveProfile(): void {
    if (this.profileForm.invalid || this.profileSaving()) return;
    const { firstName, lastName, phone } = this.profileForm.getRawValue();
    this.profileSaving.set(true);

    this.account$
      .updateAccount({
        firstName: firstName!,
        lastName: lastName!,
        phone: phone || null,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dto) => {
          this.account.set(dto);
          this.profileSaving.set(false);
          this.toast.show('Profilul a fost actualizat.', 'success');
          this.cdr.markForCheck();
        },
        error: () => {
          this.profileSaving.set(false);
          this.toast.show('Eroare la salvarea profilului.', 'error');
          this.cdr.markForCheck();
        },
      });
  }

  changePassword(): void {
    if (this.passwordForm.invalid || this.passwordSaving()) return;
    const { currentPassword, newPassword, confirmNewPassword } = this.passwordForm.getRawValue();
    this.passwordSaving.set(true);
    this.passwordError.set(null);

    this.account$
      .changePassword({
        currentPassword: currentPassword!,
        newPassword: newPassword!,
        confirmNewPassword: confirmNewPassword!,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.passwordSaving.set(false);
          this.passwordForm.reset();
          this.toast.show('Parola a fost schimbată cu succes.', 'success');
          this.cdr.markForCheck();
        },
        error: (err: HttpErrorResponse) => {
          this.passwordSaving.set(false);
          if (err.status === 400) {
            this.passwordError.set('Parola actuală este incorectă.');
          } else {
            this.passwordError.set('Eroare la schimbarea parolei.');
          }
          this.cdr.markForCheck();
        },
      });
  }

  requestDeletion(): void {
    if (this.deletionSaving()) return;
    this.deletionSaving.set(true);

    this.account$
      .requestDeletion()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletionSaving.set(false);
          this.showDeleteConfirm.set(false);
          const current = this.account();
          if (current) this.account.set({ ...current, deletionRequested: true });
          this.toast.show('Cererea de ștergere a contului a fost înregistrată.', 'info');
          this.cdr.markForCheck();
        },
        error: () => {
          this.deletionSaving.set(false);
          this.toast.show('Eroare la procesarea cererii.', 'error');
          this.cdr.markForCheck();
        },
      });
  }
}
