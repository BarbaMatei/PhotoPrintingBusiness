import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

const CONSENT_KEY = 'cookie-consent';

export type CookieConsent = 'all' | 'essential';

@Component({
  selector: 'app-cookie-consent',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (visible()) {
      <div class="cookie-banner" role="dialog" aria-label="Consimțământ cookie-uri">
        <div class="cookie-banner__content">
          <p class="cookie-banner__text">
            Folosim cookie-uri pentru a îmbunătăți experiența ta pe site. Poți accepta toate cookie-urile
            sau doar pe cele esențiale pentru funcționarea site-ului.
            <a routerLink="/legal/cookies" class="cookie-banner__link">Politica de cookies</a>
          </p>
          <div class="cookie-banner__actions">
            <button class="btn btn--primary" (click)="accept('all')">Accept toate</button>
            <button class="btn btn--ghost" (click)="accept('essential')">Doar esențiale</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .cookie-banner {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background: #fff;
      border-top: 1px solid #e5e7eb;
      box-shadow: 0 -4px 24px rgba(0, 0, 0, 0.08);
      z-index: 1000;
      padding: 1rem 1.5rem;
    }

    .cookie-banner__content {
      max-width: 900px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      gap: 1.5rem;
      flex-wrap: wrap;
    }

    .cookie-banner__text {
      flex: 1;
      font-size: 0.9rem;
      color: #374151;
      margin: 0;
      min-width: 200px;
    }

    .cookie-banner__link {
      color: #16a34a;
      text-decoration: underline;
      white-space: nowrap;
    }

    .cookie-banner__actions {
      display: flex;
      gap: 0.75rem;
      flex-shrink: 0;
      flex-wrap: wrap;
    }

    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.5rem 1.25rem;
      border-radius: 8px;
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
      border: none;
      white-space: nowrap;

      &--primary {
        background: #16a34a;
        color: #fff;
        &:hover { background: #15803d; }
      }

      &--ghost {
        background: transparent;
        color: #6b7280;
        border: 1px solid #d1d5db;
        &:hover { background: #f9fafb; }
      }
    }
  `],
})
export class CookieConsentBanner {
  readonly visible = signal(this.shouldShow());

  accept(choice: CookieConsent): void {
    localStorage.setItem(CONSENT_KEY, choice);
    this.visible.set(false);
  }

  private shouldShow(): boolean {
    try {
      return localStorage.getItem(CONSENT_KEY) === null;
    } catch {
      return false;
    }
  }
}
