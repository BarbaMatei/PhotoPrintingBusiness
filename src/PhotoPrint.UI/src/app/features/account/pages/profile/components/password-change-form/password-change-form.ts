import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { PasswordChecklistComponent } from '../../../../../../shared/components/password-checklist/password-checklist.component';

@Component({
  selector: 'app-password-change-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, PasswordChecklistComponent],
  templateUrl: './password-change-form.html',
  styleUrl: './password-change-form.scss',
})
export class PasswordChangeForm {
  readonly form = input.required<FormGroup>();
  readonly saving = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly submitted = output<void>();

  isInvalid(field: string): boolean {
    const ctrl = this.form().get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }
}
