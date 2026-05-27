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
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
})
export class LoginPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  loading = false;
  showPassword = false;
  formError: string | null = null;
  resendVisible = false;
  resendEmail = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    rememberMe: [false],
  });

  submit(): void {
    if (this.form.invalid || this.loading) return;
    this.formError = null;
    this.resendVisible = false;

    this.loading = true;
    const { email, password } = this.form.getRawValue();

    this.auth
      .login(email!, password!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          const url = this.auth.getReturnUrl();
          this.auth.setReturnUrl('/tipareste');
          this.router.navigateByUrl(url);
        },
        error: (err: HttpErrorResponse) => {
          this.loading = false;
          if (err.status === 401) {
            this.formError = 'Email sau parolă incorectă.';
          } else if (err.status === 403) {
            this.formError = 'Confirmați adresa de email pentru a continua.';
            this.resendVisible = true;
            this.resendEmail = email!;
          } else if (err.status === 423) {
            const minutes: number = (err.error as { remainingMinutes?: number })?.remainingMinutes ?? 0;
            this.formError = `Contul este blocat. Încercați din nou în ${minutes} minute.`;
          }
          this.cdr.markForCheck();
        },
      });
  }

  resendConfirmation(): void {
    if (!this.resendEmail) return;
    this.auth.resendConfirmation(this.resendEmail).subscribe({
      next: () => this.toast.show('Email de confirmare retrimis.', 'success'),
      error: () => this.toast.show('Nu s-a putut trimite emailul. Încercați mai târziu.', 'error'),
    });
  }
}
