import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../../core/services/auth.service';
import {
  passwordStrengthValidator,
  passwordMatchValidator,
  PasswordStrengthErrors,
} from '../../../../shared/validators/password-strength.validator';

@Component({
  selector: 'app-reset-password-page',
  templateUrl: './reset-password-page.html',
  styleUrl: './reset-password-page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
})
export class ResetPasswordPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly userId = this.route.snapshot.queryParamMap.get('userId');
  readonly token = this.route.snapshot.queryParamMap.get('token');
  readonly paramsValid = !!(this.userId && this.token);

  loading = false;
  showPassword = false;
  succeeded = false;
  apiError: string | null = null;

  form = this.fb.group(
    {
      password: ['', [Validators.required, passwordStrengthValidator]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordMatchValidator },
  );

  get strengthErrors(): PasswordStrengthErrors | null {
    return this.form.get('password')?.errors?.['passwordStrength'] ?? null;
  }

  submit(): void {
    if (this.form.invalid || this.loading || !this.paramsValid) return;
    this.loading = true;
    this.apiError = null;

    const v = this.form.getRawValue();

    this.auth
      .resetPassword({
        userId: this.userId!,
        token: this.token!,
        newPassword: v.password!,
        confirmPassword: v.confirmPassword!,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loading = false;
          this.succeeded = true;
          this.cdr.markForCheck();
        },
        error: (err: HttpErrorResponse) => {
          this.loading = false;
          if (err.status === 400) {
            this.apiError = (err.error as { detail?: string })?.detail ?? 'Link invalid sau expirat.';
          }
          this.cdr.markForCheck();
        },
      });
  }
}
