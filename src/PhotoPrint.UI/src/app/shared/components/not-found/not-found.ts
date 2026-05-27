import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `
    <div class="not-found">
      <h1 class="not-found__code">404</h1>
      <p class="not-found__message">Pagina nu a fost găsită.</p>
      <a routerLink="/" class="not-found__link">Înapoi acasă</a>
    </div>
  `,
  styles: [`
    .not-found {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 60vh;
      text-align: center;
      padding: 2rem;
    }
    .not-found__code {
      font-size: 6rem;
      font-weight: 700;
      color: #1a73e8;
      margin: 0;
    }
    .not-found__message {
      font-size: 1.25rem;
      color: #3c4043;
      margin: 0.5rem 0 2rem;
    }
    .not-found__link {
      color: #1a73e8;
      font-weight: 500;
      text-decoration: none;
    }
    .not-found__link:hover { text-decoration: underline; }
  `],
})
export class NotFound {}
