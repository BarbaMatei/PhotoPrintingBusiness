import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
  computed,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../../../core/services/product.service';
import { UploadService, BatchUploadItemResult } from '../../../../core/services/upload.service';
import { CartService } from '../../../../core/services/cart.service';
import { AuthService } from '../../../../core/services/auth.service';
import { GuestAuthService } from '../../../../core/services/guest-auth.service';
import { Product, ProductSize, PriceResult } from '../../../../core/models/product.model';
import { UploadState, UploadDto } from '../../../../core/models/upload.model';
import { calcPrice } from '../../../../shared/utils/pricing.utils';
import { PhotoUploadComponent, FileValidationError } from '../../components/photo-upload/photo-upload.component';
import { PhotoThumbnailComponent } from '../../components/photo-thumbnail/photo-thumbnail.component';
import { QuantityStepperComponent } from '../../components/quantity-stepper/quantity-stepper.component';
import { OrderSummaryComponent } from '../../components/order-summary/order-summary.component';
import { PhotoLightboxComponent } from '../../../../shared/components/photo-lightbox/photo-lightbox.component';

@Component({
  selector: 'app-format-selector-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PhotoUploadComponent,
    PhotoThumbnailComponent,
    QuantityStepperComponent,
    OrderSummaryComponent,
    PhotoLightboxComponent,
  ],
  templateUrl: './format-selector-page.html',
  styleUrl: './format-selector-page.scss',
})
export class FormatSelectorPage implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly uploadService = inject(UploadService);
  private readonly cartService = inject(CartService);
  private readonly authService = inject(AuthService);
  private readonly guestAuthService = inject(GuestAuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  product: Product | null = null;
  loading = true;
  error: string | null = null;

  /** sessionStorage key scoped to this product; set in ngOnInit. */
  private storageKey = '';

  /** All upload states (pending, uploading, done, error). */
  readonly uploads = signal<UploadState[]>([]);

  /** Validation errors shown below the upload zone. */
  uploadErrors: FileValidationError[] = [];

  /** Shows a brief confirmation after adding to cart. */
  cartSuccess = false;

  readonly selectedSize = signal<ProductSize | null>(null);

  /** src for the full-resolution lightbox overlay; null = closed. */
  readonly lightboxSrc = signal<string | null>(null);

  /** Total copies across all done uploads — drives tier-based pricing. */
  readonly totalQuantity = computed(() =>
    this.uploads().filter(u => u.status === 'done').reduce((s, u) => s + u.quantity, 0),
  );

  readonly priceResult = computed<PriceResult | null>(() => {
    const size = this.selectedSize();
    const qty = this.totalQuantity();
    if (!size || qty < 1) return null;
    return calcPrice(size.pricingTiers, qty);
  });

  readonly canAddToCart = computed(() =>
    this.selectedSize() !== null &&
    this.totalQuantity() > 0 &&
    this.priceResult() !== null,
  );

  /** Human-readable reason why the button is currently disabled. */
  readonly disabledReason = computed<string | null>(() => {
    if (this.canAddToCart()) return null;
    if (!this.selectedSize()) return 'Alege o dimensiune pentru a continua.';
    if (this.totalQuantity() === 0) return 'Adaugă cel puțin o fotografie.';
    return 'Prețul nu este disponibil pentru combinația selectată.';
  });

  /** Count of successfully uploaded photos (passed to upload zone for limit check). */
  get doneUploadCount(): number {
    return this.uploads().filter(u => u.status === 'done').length;
  }

  form = this.fb.group({
    sizeId: ['', Validators.required],
    finish: [''],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/tipareste']);
      return;
    }

    this.storageKey = `photoprint-uploads-${id}`;

    // Ensure the user has a valid auth token before they try to upload.
    // If not logged in and no guest token exists, create an anonymous pre-session
    // so the upload endpoint (DualAuthPolicy) will accept the request.
    if (!this.authService.isAuthenticated() && !this.authService.getGuestToken()) {
      this.guestAuthService.initAnonymousSession().subscribe({
        next: res => {
          this.guestAuthService.storeSession({
            guestToken: res.guestToken,
            firstName: '',
            lastName: '',
            email: '',
            phone: '',
          });
        },
        error: () => {
          // Non-fatal: user will see a 401 on upload and can try again.
        },
      });
    }

    this.productService.getProduct(id).subscribe({
      next: product => {
        this.product = product;
        this.loading = false;

        if (product.finishes.length > 0) {
          this.form.patchValue({ finish: product.finishes[0] });
        }
        const activeSizes = product.sizes;
        if (activeSizes.length === 1) {
          this.form.patchValue({ sizeId: activeSizes[0].id });
          this.selectedSize.set(activeSizes[0]);
        }
        this.restoreFromSession();
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'Produsul nu a fost găsit.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });

    this.form.get('sizeId')!.valueChanges.subscribe(id => {
      const size = this.product?.sizes.find(s => s.id === id) ?? null;
      this.selectedSize.set(size);
      this.cdr.markForCheck();
    });
  }

  onFilesAccepted(files: File[]): void {
    if (files.length === 0) return;

    const newStates: UploadState[] = files.map(file => ({
      clientId: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
      file,
      progress: 0,
      status: 'uploading' as const,
      quantity: 1,
    }));
    this.uploads.update(prev => [...prev, ...newStates]);
    this.cdr.markForCheck();

    if (files.length === 1) {
      // Single file: use individual upload for accurate per-file progress.
      const { clientId } = newStates[0];
      this.uploadService.upload(files[0]).subscribe({
        next: event => {
          if (event.type === 'progress') {
            this.updateUpload(clientId, { progress: event.progress ?? 0 });
          } else if (event.type === 'done') {
            this.updateUpload(clientId, { status: 'done', progress: 100, dto: event.dto });
          }
        },
        error: () => {
          this.updateUpload(clientId, { status: 'error', error: 'Eroare la încărcarea fișierului.' });
        },
      });
    } else {
      // Multiple files: send all in one request.
      const clientIds = newStates.map(s => s.clientId);
      this.uploadService.uploadBatch(files).subscribe({
        next: event => {
          if (event.type === 'progress') {
            // Reflect overall upload progress on every file in the batch.
            clientIds.forEach(id => this.updateUpload(id, { progress: event.progress ?? 0 }));
          } else if (event.type === 'done') {
            event.results!.forEach((result: BatchUploadItemResult, i: number) => {
              if (result.upload) {
                this.updateUpload(clientIds[i], { status: 'done', progress: 100, dto: result.upload });
              } else {
                this.updateUpload(clientIds[i], { status: 'error', error: result.error ?? 'Eroare la încărcarea fișierului.' });
              }
            });
          }
        },
        error: () => {
          clientIds.forEach(id =>
            this.updateUpload(id, { status: 'error', error: 'Eroare la încărcarea fișierului.' }),
          );
        },
      });
    }
  }

  onFilesRejected(errors: FileValidationError[]): void {
    this.uploadErrors = [...this.uploadErrors, ...errors];
    this.cdr.markForCheck();
  }

  onRemoveUpload(clientId: string): void {
    this.uploads.update(prev => prev.filter(u => u.clientId !== clientId));
    this.saveToSession();
    this.cdr.markForCheck();
  }

  onQuantityChange(clientId: string, quantity: number): void {
    this.updateUpload(clientId, { quantity });
  }

  onAddToCart(): void {
    if (!this.canAddToCart() || !this.product) return;

    const doneUploads = this.uploads().filter(u => u.status === 'done' && u.dto);
    const newItems = doneUploads.map(u => ({ uploadId: u.dto!.id, quantity: u.quantity }));

    // Preserve existing cart items for the same product+size so the user can add
    // multiple batches without losing previously added photos.
    const currentCart = this.cartService.snapshot;
    const selectedSize = this.selectedSize()!;
    const finishName = this.form.value.finish || null;
    const existingGroup = currentCart.groups.find(g =>
      g.productId === this.product!.id && g.sizeId === selectedSize.id && g.finishName === finishName,
    );
    const existingItems = existingGroup?.items.map(i => ({ uploadId: i.uploadId, quantity: i.quantity })) ?? [];

    const allItems = [...existingItems, ...newItems];

    this.cartService.setCart({ productId: this.product!.id, sizeId: selectedSize.id, finishName, items: allItems }).subscribe({
      next: () => {
        // Clear uploads so the user can immediately add another batch of photos
        // (different format/finish) without navigating away.
        this.uploads.set([]);
        sessionStorage.removeItem(this.storageKey);
        this.uploadErrors = [];
        this.cartSuccess = true;
        this.cdr.markForCheck();
        // Auto-hide success banner after 3 s
        setTimeout(() => {
          this.cartSuccess = false;
          this.cdr.markForCheck();
        }, 3000);
      },
      error: () => {
        this.error = 'Nu am putut adăuga în coș. Încearcă din nou.';
        this.cdr.markForCheck();
      },
    });
  }

  dismissUploadError(index: number): void {
    this.uploadErrors = this.uploadErrors.filter((_, i) => i !== index);
    this.cdr.markForCheck();
  }

  private updateUpload(clientId: string, patch: Partial<UploadState>): void {
    this.uploads.update(prev => prev.map(u =>
      u.clientId === clientId ? { ...u, ...patch } : u,
    ));
    this.saveToSession();
    this.cdr.markForCheck();
  }

  /** Persists done uploads (dto + quantity) to sessionStorage for refresh recovery. */
  private saveToSession(): void {
    if (!this.storageKey) return;
    const toSave = this.uploads()
      .filter(u => u.status === 'done' && u.dto)
      .map(u => ({ clientId: u.clientId, dto: u.dto!, quantity: u.quantity }));
    if (toSave.length > 0) {
      sessionStorage.setItem(this.storageKey, JSON.stringify(toSave));
    } else {
      sessionStorage.removeItem(this.storageKey);
    }
  }

  /** Restores done uploads from sessionStorage and fetches their preview blobs. */
  private restoreFromSession(): void {
    if (!this.storageKey) return;
    const raw = sessionStorage.getItem(this.storageKey);
    if (!raw) return;
    try {
      const saved: Array<{ clientId: string; dto: UploadDto; quantity: number }> = JSON.parse(raw);
      if (!saved.length) return;

      // Immediately show placeholders so the grid appears right away.
      const restored: UploadState[] = saved.map(s => ({
        clientId: s.clientId,
        progress: 100,
        status: 'done' as const,
        dto: s.dto,
        quantity: s.quantity,
      }));
      this.uploads.set(restored);
      this.cdr.markForCheck();

      // Fetch preview blobs in parallel; remove any upload whose preview fails.
      saved.forEach(s => {
        this.uploadService.getPreviewBlob(s.dto.id).subscribe({
          next: url => this.updateUpload(s.clientId, { previewUrl: url }),
          error: () => {
            // Auth may have changed or upload expired — drop the stale entry.
            this.uploads.update(prev => prev.filter(u => u.clientId !== s.clientId));
            this.saveToSession();
            this.cdr.markForCheck();
          },
        });
      });
    } catch {
      sessionStorage.removeItem(this.storageKey);
    }
  }
}
