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
import { PersonalInfoForm } from './components/personal-info-form/personal-info-form';
import { PasswordChangeForm } from './components/password-change-form/password-change-form';
import { AccountDeletionCard } from './components/account-deletion-card/account-deletion-card';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SpinnerComponent, PersonalInfoForm, PasswordChangeForm, AccountDeletionCard],
  template: `
    <div class="profile-page">
      <h1 class="page-title">Profilul meu</h1>

      @if (loading()) {
        <app-spinner label="Se încarcă..." [showLabel]="true" />
      }

      @if (!loading() && account()) {
        <app-personal-info-form
          [form]="profileForm"
          [email]="account()!.email"
          [saving]="profileSaving()"
          (submitted)="saveProfile()"
        />

        @if (account()!.hasPassword) {
          <app-password-change-form
            [form]="passwordForm"
            [saving]="passwordSaving()"
            [errorMessage]="passwordError()"
            (submitted)="changePassword()"
          />
        }

        <app-account-deletion-card
          [deletionRequested]="account()!.deletionRequested"
          [saving]="deletionSaving()"
          (confirmed)="requestDeletion()"
        />
      }
    </div>
  `,
  styles: [
    `
      .profile-page {
        padding-bottom: 3rem;
      }

      .page-title {
        font-size: 1.5rem;
        font-weight: 700;
        margin-bottom: 1.5rem;
      }

    `,
  ],
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
    phone: ['', [Validators.pattern(/^07[0-9]{8}$/)]],
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
    },
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
