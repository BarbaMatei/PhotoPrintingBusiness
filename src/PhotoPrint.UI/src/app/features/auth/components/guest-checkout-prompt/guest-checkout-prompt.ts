import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  OnInit,
  Output,
  ViewChild,
  inject,
} from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { GuestCheckoutFormComponent } from '../guest-checkout-form/guest-checkout-form';

type Step = 'options' | 'guest-form';

@Component({
  selector: 'app-guest-checkout-prompt',
  templateUrl: './guest-checkout-prompt.html',
  styleUrl: './guest-checkout-prompt.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [GuestCheckoutFormComponent],
})
export class GuestCheckoutPromptComponent implements OnInit {
  @Output() readonly dismissed = new EventEmitter<void>();
  @Output() readonly guestSessionReady = new EventEmitter<void>();

  @ViewChild('dialog') private dialogRef!: ElementRef<HTMLDialogElement>;

  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  step: Step = 'options';

  ngOnInit(): void {
    // Dialog opens via template reference — ensure it shows as modal
    setTimeout(() => this.dialogRef?.nativeElement.showModal(), 0);
  }

  goToLogin(): void {
    this.auth.setReturnUrl('/checkout');
    this.dialogRef.nativeElement.close();
    this.router.navigate(['/auth/login']);
  }

  goToRegister(): void {
    this.dialogRef.nativeElement.close();
    this.router.navigate(['/auth/register']);
  }

  showGuestForm(): void {
    this.step = 'guest-form';
  }

  onGuestSessionCreated(): void {
    this.dialogRef.nativeElement.close();
    this.guestSessionReady.emit();
  }

  close(): void {
    this.dialogRef.nativeElement.close();
    this.dismissed.emit();
  }
}
