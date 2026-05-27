import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../../core/services/auth.service';
import {
  passwordStrengthValidator,
  passwordMatchValidator,
  PasswordStrengthErrors,
} from '../../../../shared/validators/password-strength.validator';
import { PasswordChecklistComponent } from '../../../../shared/components/password-checklist/password-checklist.component';

@Component({
  selector: 'app-register-page',
  templateUrl: './register-page.html',
  styleUrl: './register-page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, PasswordChecklistComponent],
})
export class RegisterPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  loading = false;
  showPassword = false;

  form = this.fb.group(
    {
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, passwordStrengthValidator]],
      confirmPassword: ['', Validators.required],
      phone: ['', Validators.pattern(/^07[0-9]{8}$/)],
      gdprConsent: [false, Validators.requiredTrue],
    },
    { validators: passwordMatchValidator },
  );

  get strengthErrors(): PasswordStrengthErrors | null {
    return this.form.get('password')?.errors?.['passwordStrength'] ?? null;
  }

  submit(): void {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    const v = this.form.getRawValue();

    this.auth
      .register({
        firstName: v.firstName!,
        lastName: v.lastName!,
        email: v.email!,
        password: v.password!,
        confirmPassword: v.confirmPassword!,
        phone: v.phone || null,
        gdprConsentAccepted: true,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.router.navigate(['/auth/verify-email'], {
            state: { email: v.email },
          });
        },
        error: (err: HttpErrorResponse) => {
          this.loading = false;
          if (err.status === 409) {
            this.form.get('email')?.setErrors({ conflict: true });
          } else if (err.status === 422 && err.error?.errors) {
            this.mapApiErrors(err.error.errors as Record<string, string[]>);
          }
          this.cdr.markForCheck();
        },
      });
  }

  private mapApiErrors(errors: Record<string, string[]>): void {
    for (const [field, messages] of Object.entries(errors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      this.form.get(key)?.setErrors({ apiError: messages[0] });
    }
  }
}
