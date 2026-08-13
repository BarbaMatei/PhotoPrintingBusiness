import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
  signal,
} from '@angular/core';

export interface FileValidationError {
  fileName: string;
  reason: 'type' | 'size' | 'limit';
}

export const MAX_FILE_SIZE_BYTES = 52_428_800; // 50 MB
export const MAX_UPLOAD_COUNT = 100;
// HEIC dropped: the API stack has no HEIF decoder, so a.heic upload only
// fails later at decode. Re-add '.heic' here and in the accept attr / hint once decode lands.
export const ACCEPTED_EXTENSIONS = new Set(['.jpg', '.jpeg', '.png']);

/** Returns the lowercase extension including the dot, e.g. '.jpg'. */
export function getExtension(fileName: string): string {
  const dot = fileName.lastIndexOf('.');
  return dot >= 0 ? fileName.slice(dot).toLowerCase() : '';
}

@Component({
  selector: 'app-photo-upload',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="upload-zone"
      [class.upload-zone--drag-over]="isDragOver()"
      (dragover)="onDragOver($event)"
      (dragleave)="onDragLeave()"
      (drop)="onDrop($event)"
      (click)="fileInput.click()"
      role="button"
      tabindex="0"
      (keydown.enter)="fileInput.click()"
      (keydown.space)="fileInput.click()"
      aria-label="Încarcă fotografii"
    >
      <input
        #fileInput
        type="file"
        accept=".jpg,.jpeg,.png"
        multiple
        hidden
        (change)="onFileInputChange($event)"
      />
      <div class="upload-zone__content">
        <div class="upload-zone__icon-wrap">
          <span class="upload-zone__icon">📷</span>
        </div>
        <p class="upload-zone__text">
          Trage fotografiile aici sau <strong>alege fișiere</strong>
        </p>
        <p class="upload-zone__hint">JPG, PNG &middot; max 50 MB/fișier &middot; max 100 fotografii</p>
      </div>
    </div>
  `,
  styles: [`
    .upload-zone {
      border: 2px dashed #dadce0;
      border-radius: 16px;
      padding: 2.5rem 2rem;
      text-align: center;
      cursor: pointer;
      transition: border-color 0.2s, background 0.2s, transform 0.15s;
      background: #fafafa;
      outline: none;

      &:hover, &:focus-visible {
        border-color: #1a73e8;
        background: #e8f0fe;
        transform: translateY(-1px);
      }
    }
    .upload-zone--drag-over {
      border-color: #1a73e8;
      background: #e8f0fe;
      transform: scale(1.01);
    }
    .upload-zone__icon-wrap {
      width: 64px;
      height: 64px;
      border-radius: 50%;
      background: linear-gradient(135deg, #e8f0fe 0%, #d2e3fc 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1rem;
    }
    .upload-zone__icon { font-size: 2rem; line-height: 1; }
    .upload-zone__text { margin: 0 0 0.35rem; font-size: 1rem; color: #3c4043; }
    .upload-zone__text strong { color: #1a73e8; }
    .upload-zone__hint { margin: 0; font-size: 0.8rem; color: #5f6368; }
  `],
})
export class PhotoUploadComponent {
  /** Emits accepted File objects. */
  @Output() filesAccepted = new EventEmitter<File[]>();

  /** Emits validation errors for rejected files. */
  @Output() filesRejected = new EventEmitter<FileValidationError[]>();

  protected readonly isDragOver = signal(false);

  /** Current count of accepted uploads (caller must keep this in sync). */
  @Input() currentUploadCount = 0;

  @HostListener('dragover', ['$event'])
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  @HostListener('dragleave')
  onDragLeave(): void {
    this.isDragOver.set(false);
  }

  @HostListener('drop', ['$event'])
  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
    const files = Array.from(event.dataTransfer?.files ?? []);
    this.processFiles(files);
  }

  onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    this.processFiles(files);
    // Reset input so the same file can be re-added after removal
    input.value = '';
  }

  /**
   * Validates and partitions files into accepted and rejected groups.
   * Exposed as public so tests can call it directly.
   */
  processFiles(files: File[]): void {
    const accepted: File[] = [];
    const rejected: FileValidationError[] = [];

    for (const file of files) {
      const ext = getExtension(file.name);

      if (!ACCEPTED_EXTENSIONS.has(ext)) {
        rejected.push({ fileName: file.name, reason: 'type' });
        continue;
      }

      if (file.size > MAX_FILE_SIZE_BYTES) {
        rejected.push({ fileName: file.name, reason: 'size' });
        continue;
      }

      if (this.currentUploadCount + accepted.length >= MAX_UPLOAD_COUNT) {
        rejected.push({ fileName: file.name, reason: 'limit' });
        continue;
      }

      accepted.push(file);
    }

    if (accepted.length > 0) {
      this.filesAccepted.emit(accepted);
    }
    if (rejected.length > 0) {
      this.filesRejected.emit(rejected);
    }
  }
}
