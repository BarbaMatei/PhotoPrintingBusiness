import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-account-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="account-layout">
      <nav class="account-nav">
        <a
          routerLink="profil"
          routerLinkActive="account-nav__link--active"
          class="account-nav__link"
          >Profilul meu</a
        >
        <a
          routerLink="adrese"
          routerLinkActive="account-nav__link--active"
          class="account-nav__link"
          >Adrese salvate</a
        >
      </nav>
      <div class="account-content">
        <router-outlet />
      </div>
    </div>
  `,
  styles: [
    `
      .account-layout {
        max-width: 900px;
        margin: 2rem auto;
        padding: 0 1rem;
        display: grid;
        grid-template-columns: 200px 1fr;
        gap: 2rem;
      }

      @media (max-width: 640px) {
        .account-layout {
          grid-template-columns: 1fr;
        }
      }

      .account-nav {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
      }

      .account-nav__link {
        display: block;
        padding: 0.625rem 1rem;
        border-radius: 8px;
        color: #374151;
        text-decoration: none;
        font-size: 0.9375rem;
        font-weight: 500;
        transition: background 0.15s;

        &:hover {
          background: #f3f4f6;
        }

        &--active {
          background: #f0fdf4;
          color: #15803d;
          font-weight: 600;
        }
      }

      .account-content {
        min-width: 0;
      }
    `,
  ],
})
export class AccountLayout {}
