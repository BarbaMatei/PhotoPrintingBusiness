import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-account-deletion-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './account-deletion-card.html',
  styleUrl: './account-deletion-card.scss',
})
export class AccountDeletionCard {
  readonly deletionRequested = input(false);
  readonly saving = input(false);

  readonly confirmed = output<void>();

  readonly showDeleteConfirm = signal(false);
}
