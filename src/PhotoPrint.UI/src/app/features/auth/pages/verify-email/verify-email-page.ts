import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnDestroy,
  inject,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-verify-email-page',
  templateUrl: './verify-email-page.html',
  styleUrl: './verify-email-page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
})
export class EmailVerificationPendingPage implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly email: string = (history.state as { email?: string })?.email ?? '';
  readonly confirmed = this.route.snapshot.queryParamMap.get('confirmed') === 'true';

  loading = false;
  countdown = 0;
  private countdownInterval?: ReturnType<typeof setInterval>;

  resend(): void {
    if (!this.email || this.loading || this.countdown > 0) return;

    this.loading = true;
    this.auth
      .resendConfirmation(this.email)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loading = false;
          this.toast.show('Email de confirmare retrimis.', 'success');
          this.cdr.markForCheck();
        },
        error: (err: HttpErrorResponse) => {
          this.loading = false;
          if (err.status === 429) {
            this.toast.show('Prea multe încercări. Așteptați câteva minute.', 'warning');
            this.startCountdown(60);
          }
          this.cdr.markForCheck();
        },
      });
  }

  private startCountdown(seconds: number): void {
    this.countdown = seconds;
    this.countdownInterval = setInterval(() => {
      this.countdown--;
      if (this.countdown <= 0) {
        this.countdown = 0;
        clearInterval(this.countdownInterval);
      }
      this.cdr.markForCheck();
    }, 1000);
  }

  ngOnDestroy(): void {
    clearInterval(this.countdownInterval);
  }
}
