import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="spinner"
      [class]="'spinner--' + size()"
      role="status"
      [attr.aria-label]="label()"
    >
      <svg class="spinner__svg" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <circle class="spinner__track" cx="12" cy="12" r="10" stroke-width="2.5" />
        <circle class="spinner__arc" cx="12" cy="12" r="10" stroke-width="2.5"
          stroke-linecap="round" stroke-dasharray="47 110" />
      </svg>
      @if (label() && showLabel()) {
        <span class="spinner__label">{{ label() }}</span>
      }
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .spinner {
      display: inline-flex;
      flex-direction: column;
      align-items: center;
      gap: $space-3;
      color: $color-primary;
    }

    .spinner--sm  .spinner__svg { width: 20px; height: 20px; }
    .spinner--md  .spinner__svg { width: 32px; height: 32px; }
    .spinner--lg  .spinner__svg { width: 48px; height: 48px; }
    .spinner--xl  .spinner__svg { width: 64px; height: 64px; }

    .spinner__svg {
      width: 32px;
      height: 32px;
      animation: spin 0.9s linear infinite;
    }

    .spinner__track {
      stroke: currentColor;
      opacity: 0.15;
    }

    .spinner__arc {
      stroke: currentColor;
      transform-origin: center;
    }

    @keyframes spin {
      from { transform: rotate(0deg); }
      to   { transform: rotate(360deg); }
    }

    .spinner__label {
      font-size: $font-size-sm;
      color: $color-neutral-500;
      font-weight: $font-weight-medium;
    }
  `],
})
export class SpinnerComponent {
  readonly size      = input<'sm' | 'md' | 'lg' | 'xl'>('md');
  readonly label     = input<string>('Se încarcă…');
  readonly showLabel = input<boolean>(false);
}
