import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

interface Rule {
  label: string;
  pass: boolean;
}

@Component({
  selector: 'app-password-checklist',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './password-checklist.component.html',
  styleUrl: './password-checklist.component.scss',
})
export class PasswordChecklistComponent {
  readonly password = input<string>('');

  readonly hasValue = computed(() => this.password().length > 0);

  readonly rules = computed<Rule[]>(() => {
    const p = this.password();
    return [
      { label: 'Minim 8 caractere',              pass: p.length >= 8 },
      { label: 'Cel puțin o literă mare',         pass: /[A-Z]/.test(p) },
      { label: 'Cel puțin o cifră',               pass: /[0-9]/.test(p) },
      { label: 'Cel puțin un caracter special',   pass: /[^A-Za-z0-9]/.test(p) },
    ];
  });
}
