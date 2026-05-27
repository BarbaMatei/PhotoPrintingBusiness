import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password-page',
  templateUrl: './forgot-password-page.html',
  styleUrl: './forgot-password-page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
})
export class ForgotPasswordPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  loading = false;
  submitted = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  submit(): void {
    if (this.form.invalid || this.loading) return;
    this.loading = true;

    this.auth
      .forgotPassword(this.form.getRawValue().email!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loading = false;
          this.submitted = true;
          this.cdr.markForCheck();
        },
        error: () => {
          // Always show success message (anti-enumeration)
          this.loading = false;
          this.submitted = true;
          this.cdr.markForCheck();
        },
      });
  }
}
