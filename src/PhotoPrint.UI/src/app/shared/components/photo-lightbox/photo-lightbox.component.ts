import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Output,
  effect,
  input,
  signal,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'app-photo-lightbox',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (src()) {
      <div
        class="lightbox__backdrop"
        role="dialog"
        aria-modal="true"
        aria-label="Previzualizare fotografie"
        (click)="close.emit()"
        (keydown.tab)="trapFocus($event)"
        (keydown.shift.tab)="trapFocus($event)"
      >
        <button
          #closeBtn
          class="lightbox__close"
          type="button"
          (click)="close.emit()"
          title="Închide"
          aria-label="Închide"
        >✕</button>
        @if (failed()) {
          <p class="lightbox__error" (click)="$event.stopPropagation()">
            Imaginea nu a putut fi încărcată (linkul poate fi expirat). Reîncarcă pagina.
          </p>
        } @else {
          <img
            [src]="src()"
            alt="Previzualizare fotografie"
            class="lightbox__img"
            (click)="$event.stopPropagation()"
            (error)="onImgError()"
          />
        }
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

    .lightbox__error {
      color: #fff;
      max-width: 70vw;
      text-align: center;
      font-size: 0.95rem;
      line-height: 1.5;
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
      &:focus-visible { outline: 2px solid #fff; outline-offset: 2px; }
    }
  `],
})
export class PhotoLightboxComponent {
  readonly src = input<string | null>(null);
  @Output() close = new EventEmitter<void>();
  /** Fires when the <img> fails to load — the parent can refresh a stale presigned URL (F7/D5b). */
  @Output() imgError = new EventEmitter<void>();

  /** True once the current image has failed to load; reset when a new src arrives. */
  readonly failed = signal(false);

  private readonly closeBtn = viewChild<ElementRef<HTMLButtonElement>>('closeBtn');
  private previousFocus: HTMLElement | null = null;
  private wasOpen = false;
  private lastSrc: string | null = null;

  constructor() {
    // Modal focus management (F17/D33): on open, move focus into the dialog and remember the
    // trigger; on close, restore it. A new/refreshed src clears the prior load-failure state.
    effect(() => {
      const src = this.src();
      const btn = this.closeBtn();
      const open = src !== null;

      if (src !== this.lastSrc && src !== null) this.failed.set(false);
      if (open && !this.wasOpen) this.previousFocus = document.activeElement as HTMLElement | null;
      if (open && btn) btn.nativeElement.focus();
      if (!open && this.wasOpen) {
        this.previousFocus?.focus();
        this.previousFocus = null;
      }

      this.wasOpen = open;
      this.lastSrc = src;
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.src()) this.close.emit();
  }

  onImgError(): void {
    this.failed.set(true);
    this.imgError.emit();
  }

  // The close button is the only focusable control, so keep Tab / Shift+Tab on it — focus
  // never escapes the modal to the page behind the backdrop.
  trapFocus(event: Event): void {
    event.preventDefault();
    this.closeBtn()?.nativeElement.focus();
  }
}
