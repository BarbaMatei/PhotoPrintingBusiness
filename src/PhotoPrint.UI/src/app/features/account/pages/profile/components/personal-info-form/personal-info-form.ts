import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-personal-info-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './personal-info-form.html',
  styleUrl: './personal-info-form.scss',
})
export class PersonalInfoForm {
  readonly form = input.required<FormGroup>();
  readonly email = input.required<string>();
  readonly saving = input(false);

  readonly submitted = output<void>();

  isInvalid(field: string): boolean {
    const ctrl = this.form().get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }
}
