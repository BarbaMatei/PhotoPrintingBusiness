import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';

@Component({
  selector: 'app-quantity-stepper',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="stepper">
      <button
        class="stepper__btn"
        (click)="decrement()"
        [disabled]="value <= min"
        aria-label="Scade cantitatea"
      >−</button>
      <span class="stepper__value">{{ value }}</span>
      <button
        class="stepper__btn"
        (click)="increment()"
        [disabled]="value >= max"
        aria-label="Crește cantitatea"
      >+</button>
    </div>
  `,
  styles: [`
    .stepper { display: inline-flex; align-items: center; gap: 0.5rem; }
    .stepper__btn { width: 28px; height: 28px; border: 1px solid #ccc; background: #fff; border-radius: 4px; cursor: pointer; font-size: 1.1rem; line-height: 1; }
    .stepper__btn:disabled { opacity: 0.4; cursor: default; }
    .stepper__value { min-width: 2ch; text-align: center; font-weight: 600; }
  `],
})
export class QuantityStepperComponent {
  @Input() value = 1;
  @Input() min = 1;
  @Input() max = 100;
  @Output() valueChange = new EventEmitter<number>();

  increment(): void {
    if (this.value < this.max) {
      this.valueChange.emit(this.value + 1);
    }
  }

  decrement(): void {
    if (this.value > this.min) {
      this.valueChange.emit(this.value - 1);
    }
  }
}
