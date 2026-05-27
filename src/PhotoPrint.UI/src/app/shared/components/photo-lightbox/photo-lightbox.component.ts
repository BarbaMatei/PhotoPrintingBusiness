import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
} from '@angular/core';

@Component({
  selector: 'app-photo-lightbox',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (src) {
      <div class="lightbox__backdrop" (click)="close.emit()">
        <button class="lightbox__close" (click)="close.emit()" title="Închide">✕</button>
        <img
          [src]="src"
          alt="Previzualizare fotografie"
          class="lightbox__img"
          (click)="$event.stopPropagation()"
        />
      </div>
    }
  `,
  styles: [`
    .lightbox__backdrop {
      position: fixed;
      inset: 0;
      z-index: 1000;
      background: rgba(0, 0, 0, 0.85);
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .lightbox__img {
      max-width: 90vw;
      max-height: 90vh;
      object-fit: contain;
      border-radius: 4px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.6);
    }

    .lightbox__close {
      position: fixed;
      top: 16px;
      right: 20px;
      background: rgba(255, 255, 255, 0.15);
      border: none;
      color: #fff;
      font-size: 1.5rem;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      line-height: 1;
      transition: background 0.15s;

      &:hover { background: rgba(255, 255, 255, 0.3); }
    }
  `],
})
export class PhotoLightboxComponent {
  @Input() src: string | null = null;
  @Output() close = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.src) this.close.emit();
  }
}
