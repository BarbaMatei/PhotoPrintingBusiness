import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
  computed,
  input,
} from '@angular/core';
import { UploadState } from '../../../../core/models/upload.model';
import { ProductSize } from '../../../../core/models/product.model';
import { computeQuality, qualityLabel, QualityLevel } from '../../../../shared/utils/quality.utils';

@Component({
  selector: 'app-photo-thumbnail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (state.status === 'uploading' || state.status === 'pending') {
      <div class="thumbnail thumbnail--uploading">
        <img [src]="localUrl()" [alt]="displayName()" class="thumbnail__img" />
        <div class="thumbnail__progress-bar" [style.width.%]="state.progress"></div>
      </div>
    } @else if (state.status === 'done' && state.dto) {
      <div class="thumbnail" [class]="'thumbnail--' + quality()">
        <img
          [src]="localUrl()"
          [alt]="displayName()"
          class="thumbnail__img"
          (click)="preview.emit(localUrl())"
          style="cursor:pointer"
        />
        <span class="thumbnail__badge thumbnail__badge--{{ quality() }}" [title]="qualityLabelText()">
          {{ qualityBadgeIcon() }}
        </span>
        <span class="thumbnail__name">{{ truncateName(displayName()) }}</span>
        <button class="thumbnail__remove" (click)="removed.emit(state.clientId)" title="Șterge">✕</button>
      </div>
    } @else if (state.status === 'error') {
      <div class="thumbnail thumbnail--error">
        <span class="thumbnail__name">{{ truncateName(displayName()) }}</span>
        <span class="thumbnail__error">{{ state.error }}</span>
        <button class="thumbnail__remove" (click)="removed.emit(state.clientId)" title="Șterge">✕</button>
      </div>
    }
  `,
  styles: [`
    .thumbnail { position: relative; border-radius: 6px; overflow: hidden; width: 96px; }
    .thumbnail__img { width: 100%; aspect-ratio: 1; object-fit: cover; display: block; }
    .thumbnail__badge { position: absolute; top: 4px; right: 4px; font-size: 1rem; }
    .thumbnail__name { display: block; font-size: 0.7rem; padding: 2px 4px; text-overflow: ellipsis; overflow: hidden; white-space: nowrap; }
    .thumbnail__remove { position: absolute; top: 2px; left: 2px; background: rgba(0,0,0,.5); color: #fff; border: none; border-radius: 50%; width: 20px; height: 20px; cursor: pointer; font-size: 0.7rem; }
    .thumbnail--uploading { background: #f5f5f5; }
    .thumbnail__progress-bar { position: absolute; bottom: 0; left: 0; height: 4px; background: #1976d2; border-radius: 2px; transition: width 0.2s; }
    .thumbnail--error { background: #fdecea; padding: 0.5rem; }
    .thumbnail__error { font-size: 0.7rem; color: #c62828; }
  `],
})
export class PhotoThumbnailComponent implements OnDestroy {
  @Input({ required: true }) state!: UploadState;
  @Input() selectedSize: ProductSize | null = null;
  @Output() removed = new EventEmitter<string>();
  @Output() preview = new EventEmitter<string>();

  // Object URL is minted ONCE per File and cached. localUrl() is called from the template on
  // every change-detection cycle (each upload-progress event rebuilds `state`), so calling
  // URL.createObjectURL there directly leaked a fresh, unrevoked blob URL every tick and churned
  // the <img> (F8, review 043-v3). Revoked when the File changes or the component is destroyed.
  private objectUrl: string | null = null;
  private objectUrlFile: File | null = null;

  quality = computed<QualityLevel>(() => {
    if (!this.state.dto || !this.selectedSize) return 'green';
    return computeQuality(
      this.state.dto.widthPx,
      this.state.dto.heightPx,
      this.selectedSize.widthMm,
      this.selectedSize.heightMm,
    );
  });

  qualityLabelText = computed(() => qualityLabel(this.quality()));

  qualityBadgeIcon = computed(() => {
    switch (this.quality()) {
      case 'green': return '🟢';
      case 'yellow': return '🟡';
      case 'red': return '🔴';
    }
  });

  localUrl(): string {
    if (this.state.previewUrl) return this.state.previewUrl;

    const file = this.state.file!;
    if (this.objectUrl && this.objectUrlFile === file) return this.objectUrl;

    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
    this.objectUrl = URL.createObjectURL(file);
    this.objectUrlFile = file;
    return this.objectUrl;
  }

  ngOnDestroy(): void {
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
  }

  displayName(): string {
    return this.state.file?.name ?? this.state.dto?.originalFileName ?? '';
  }

  truncateName(name: string): string {
    return name.length > 20 ? name.slice(0, 17) + '…' : name;
  }
}
