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
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, of, map, tap, shareReplay, finalize } from 'rxjs';
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

    // Pre-create an anonymous guest session for not-logged-in users so the first
    // upload is accepted. ensureGuestSession() also runs per upload, so an expired
    // or cleared token self-heals on the next attempt.
    this.ensureGuestSession().subscribe({ error: () => { /* non-fatal */ } });

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

    this.ensureGuestSession().subscribe({
      next: () => this.performUpload(files, newStates),
      error: () =>
        newStates.forEach(s =>
          this.updateUpload(s.clientId, {
            status: 'error',
            error: 'Eroare la încărcarea fișierului.',
          }),
        ),
    });
  }

  /** In-flight anonymous-session init, shared so concurrent callers don't each mint a
   *  session (FE-1). Reset once it settles so a later expiry can re-init. */
  private guestInit$: Observable<void> | null = null;

  /** Logged-in users and guests with a token proceed immediately; a guest with no
   *  token gets a fresh anonymous session created first. Combined with the
   *  errorInterceptor clearing stale guest tokens on 401 and the upload/preview retry
   *  below, an expired session self-heals: the failed attempt clears the token, the
   *  retry re-inits here. */
  private ensureGuestSession(): Observable<void> {
    if (this.authService.isAuthenticated() || this.authService.getGuestToken()) {
      return of(void 0);
    }
    // Dedup concurrent inits: ngOnInit pre-creates a session, and an eager user dropping
    // files before it resolves would otherwise fire a second init (getGuestToken() is a
    // synchronous localStorage read that stays null until the first init lands) — minting a
    // duplicate session and orphaning uploads. Share one in-flight request; finalize resets
    // the field so a later expiry re-inits (FE-1).
    this.guestInit$ ??= this.guestAuthService.initAnonymousSession().pipe(
      tap(res =>
        this.guestAuthService.storeSession({
          guestToken: res.guestToken,
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
        }),
      ),
      map(() => void 0),
      finalize(() => { this.guestInit$ = null; }),
      shareReplay(1),
    );
    return this.guestInit$;
  }

  private performUpload(files: File[], newStates: UploadState[], isRetry = false): void {
    const clientIds = newStates.map(s => s.clientId);
    const failAll = () =>
      clientIds.forEach(id =>
        this.updateUpload(id, { status: 'error', error: 'Eroare la încărcarea fișierului.' }));

    // FE-2: a stale-but-present guest token isn't caught by ensureGuestSession (it only
    // checks presence, not expiry — the token is an opaque id, so expiry can't be read
    // client-side), so the upload goes out stale and 401s; the interceptor then clears the
    // token. Re-init a fresh session and retry the upload exactly once so the self-heal is
    // seamless instead of surfacing a generic error the user must resolve by re-dropping.
    const onUploadError = (err: unknown) => {
      if (!isRetry && err instanceof HttpErrorResponse && err.status === 401) {
        this.ensureGuestSession().subscribe({
          next: () => this.performUpload(files, newStates, true),
          error: () => failAll(),
        });
        return;
      }
      failAll();
    };

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
        error: onUploadError,
      });
    } else {
      // Multiple files: send all in one request.
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
        error: onUploadError,
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

      // Fetch preview blobs in parallel; a 401 self-heals, a 404 drops the stale entry.
      saved.forEach(s => this.fetchPreviewWithRetry(s.dto.id, s.clientId, false));
    } catch {
      sessionStorage.removeItem(this.storageKey);
    }
  }

  /** Fetches a restored upload's preview. On a 401 (expired guest token on refresh) the
   *  interceptor clears the token; re-init a fresh session and retry ONCE, so a refresh with
   *  an expired session doesn't wipe the whole restored grid. Only a genuine 404 drops the
   *  entry; transient failures (5xx/network, or a still-failing retry) keep it visible for a
   *  later refresh (FE-4/NEW-2). */
  private fetchPreviewWithRetry(uploadId: string, clientId: string, isRetry: boolean): void {
    this.uploadService.getPreviewBlob(uploadId).subscribe({
      next: url => this.updateUpload(clientId, { previewUrl: url }),
      error: (err: unknown) => {
        const status = err instanceof HttpErrorResponse ? err.status : null;
        if (!isRetry && status === 401) {
          // Expired guest token on refresh: the interceptor cleared it. Re-init and retry
          // once before deciding anything (FE-4). If re-init itself fails, keep the entry.
          this.ensureGuestSession().subscribe({
            next: () => this.fetchPreviewWithRetry(uploadId, clientId, true),
            error: () => { /* couldn't re-init now — keep it, a later refresh retries (NEW-2) */ },
          });
          return;
        }
        if (status === 404) {
          // The upload is genuinely gone — drop the stale entry.
          this.dropRestoredEntry(clientId);
          return;
        }
        // Transient failure (5xx / network / still-401 after retry): keep the completed
        // upload visible (without a preview) rather than erasing work; a later refresh
        // retries. Only a definitive 404 discards it (NEW-2, review 042-v2).
      },
    });
  }

  private dropRestoredEntry(clientId: string): void {
    this.uploads.update(prev => prev.filter(u => u.clientId !== clientId));
    this.saveToSession();
    this.cdr.markForCheck();
  }
}
