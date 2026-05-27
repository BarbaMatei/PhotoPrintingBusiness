import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  afterNextRender,
  inject,
} from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../services/toast.service';
import { environment } from '../../../../environments/environment';

declare const google: {
  accounts: {
    id: {
      initialize(config: Record<string, unknown>): void;
      renderButton(element: HTMLElement, options: Record<string, unknown>): void;
    };
  };
};

@Component({
  selector: 'app-google-auth-button',
  templateUrl: './google-auth-button.html',
  styleUrl: './google-auth-button.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoogleAuthButton {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  sdkLoaded = false;

  constructor() {
    afterNextRender(() => this.initGoogleSdk());
  }

  private initGoogleSdk(): void {
    if (typeof google === 'undefined' || !google?.accounts?.id) {
      // SDK not yet loaded — retry once after a short delay
      setTimeout(() => {
        if (typeof google !== 'undefined' && google?.accounts?.id) {
          this.renderButton();
        }
      }, 1500);
      return;
    }
    this.renderButton();
  }

  private renderButton(): void {
    const container = document.getElementById('google-signin-btn');
    if (!container) return;

    google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: { credential: string }) => {
        this.handleCredential(response.credential);
      },
      error_callback: () => {
        this.toast.show('Autentificarea Google a eșuat. Încearcă din nou.', 'error');
      },
    });

    google.accounts.id.renderButton(container, {
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      locale: 'ro',
      width: 300,
    });

    this.sdkLoaded = true;
    this.cdr.markForCheck();
  }

  private handleCredential(idToken: string): void {
    this.auth.googleLogin(idToken).subscribe({
      next: res => {
        if (res.accountLinked) {
          this.toast.show('Contul tău Google a fost conectat.', 'success');
        }
        const url = this.auth.getReturnUrl();
        this.auth.setReturnUrl('/tipareste');
        this.router.navigateByUrl(url);
      },
      error: () => {
        this.toast.show('Autentificarea Google a eșuat. Încearcă din nou.', 'error');
      },
    });
  }
}
