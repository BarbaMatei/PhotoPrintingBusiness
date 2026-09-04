import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ControlContainer,
  FormGroup,
  FormGroupDirective,
  ReactiveFormsModule,
} from '@angular/forms';

@Component({
  selector: 'app-address-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './address-form.html',
  styleUrl: './address-form.scss',
  viewProviders: [{ provide: ControlContainer, useExisting: FormGroupDirective }],
})
export class AddressForm implements OnInit {
  private readonly container = inject(ControlContainer);
  private readonly destroyRef = inject(DestroyRef);

  private readonly formEvents = signal(0);
  private form!: FormGroup;

  ngOnInit(): void {
    this.form = this.container.control as FormGroup;
    this.form.events
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.formEvents.update((n) => n + 1));
  }

  fi(field: string): boolean {
    this.formEvents();
    const ctrl = this.form?.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }
}
