import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LockerDto } from '../../../core/models/shipping.model';
import { LockerMapComponent } from './locker-map';

@Component({
  selector: 'app-locker-selector',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, LockerMapComponent],
  templateUrl: './locker-selector.html',
  styleUrl: './locker-selector.scss',
})
export class LockerSelector implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  readonly lockers = input.required<LockerDto[]>();
  readonly selectedLockerId = input<string | null>(null);
  readonly searchControl = input.required<FormControl<string | null>>();
  readonly searchFailed = input(false);
  readonly showError = input(false);

  readonly lockerSelected = output<LockerDto>();
  readonly retry = output<void>();

  readonly searchValue = signal('');

  ngOnInit(): void {
    const control = this.searchControl();
    this.searchValue.set(control.value ?? '');
    control.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.searchValue.set(value ?? ''));
  }
}
