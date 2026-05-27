import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="empty-state" [class]="'empty-state--' + variant()">
      @if (icon()) {
        <div class="empty-state__icon" aria-hidden="true">{{ icon() }}</div>
      }
      <h3 class="empty-state__title">{{ title() }}</h3>
      @if (message()) {
        <p class="empty-state__message">{{ message() }}</p>
      }
      @if (actionLabel() && actionLink()) {
        <a [routerLink]="actionLink()" class="btn btn--primary btn--lg">
          {{ actionLabel() }}
        </a>
      }
      @if (actionLabel() && !actionLink()) {
        <button type="button" class="btn btn--primary btn--lg" (click)="action.emit()">
          {{ actionLabel() }}
        </button>
      }
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: $space-16 $space-6;
      gap: $space-3;
    }

    .empty-state--compact {
      padding: $space-8 $space-4;
    }

    .empty-state__icon {
      font-size: 3.5rem;
      line-height: 1;
      margin-bottom: $space-2;
      filter: grayscale(0.2);
    }

    .empty-state__title {
      font-size: $font-size-xl;
      font-weight: $font-weight-bold;
      color: $color-neutral-900;
      margin: 0;
    }

    .empty-state__message {
      font-size: $font-size-base;
      color: $color-neutral-500;
      max-width: 380px;
      line-height: $line-height-relaxed;
      margin: 0 0 $space-2;
    }

    .empty-state--error .empty-state__title {
      color: $color-error;
    }
  `],
})
export class EmptyStateComponent {
  readonly variant     = input<'default' | 'error' | 'compact'>('default');
  readonly icon        = input<string>('');
  readonly title       = input.required<string>();
  readonly message     = input<string>('');
  readonly actionLabel = input<string>('');
  readonly actionLink  = input<string>('');

  readonly action = output<void>();
}
