import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  EventEmitter,
  Output,
  inject,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { GuestAuthService } from '../../../../core/services/guest-auth.service';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-guest-checkout-form',
  templateUrl: './guest-checkout-form.html',
  styleUrl: './guest-checkout-form.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
})
export class GuestCheckoutFormComponent {
  @Output() readonly sessionCreated = new EventEmitter<void>();
  @Output() readonly cancelled = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly guestAuth = inject(GuestAuthService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  loading = false;

  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.pattern(/^07[0-9]{8}$/)]],
  });

  submit(): void {
    if (this.form.invalid || this.loading) return;
    this.loading = true;

    const v = this.form.getRawValue();
    const dto = {
      firstName: v.firstName!,
      lastName: v.lastName!,
      email: v.email!,
      phone: v.phone!,
    };

    const existingSession = this.guestAuth.getStoredSession();

    const action$ = existingSession?.guestToken
      // Anonymous pre-session exists — just fill in the contact info
      ? this.guestAuth.updateContactInfo(dto).pipe(
          // map void → shape expected by next handler
          map(() => ({ guestToken: existingSession.guestToken })),
        )
      // No session yet — create a full one
      : this.guestAuth.createGuestSession(dto);

    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.guestAuth.storeSession({ guestToken: res.guestToken, ...dto });
        this.loading = false;
        this.sessionCreated.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        if (err.status === 422) {
          this.toast.show('Vă rugăm verificați datele introduse.', 'warning');
        } else {
          this.toast.show('Eroare la creare sesiune. Încercați din nou.', 'error');
        }
      },
    });
  }
}
