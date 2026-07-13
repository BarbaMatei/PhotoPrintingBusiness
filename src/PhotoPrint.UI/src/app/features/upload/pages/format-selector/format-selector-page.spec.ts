import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Routes, ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError, Subject } from 'rxjs';
import { FormatSelectorPage } from './format-selector-page';
import { ProductService } from '../../../../core/services/product.service';
import { AuthService } from '../../../../core/services/auth.service';
import { GuestAuthService } from '../../../../core/services/guest-auth.service';
import { UploadService } from '../../../../core/services/upload.service';
import { CartService } from '../../../../core/services/cart.service';
import { Product } from '../../../../core/models/product.model';
import { UploadState, UploadDto } from '../../../../core/models/upload.model';

const TEST_ROUTES: Routes = [{ path: '**', redirectTo: '' }];

const MOCK_PRODUCT: Product = {
  id: 'p1',
  name: 'Poze foto',
  productType: 'PhotoPrint',
  imageUrl: null,
  sortOrder: 0,
  sizes: [
    {
      id: 's1',
      label: '10×15',
      widthMm: 100,
      heightMm: 150,
      pricingTiers: [
        { minQuantity: 1, maxQuantity: 9, unitPrice: 2.50 },
        { minQuantity: 10, maxQuantity: null, unitPrice: 1.80 },
      ],
    },
    {
      id: 's2',
      label: '13×18',
      widthMm: 130,
      heightMm: 180,
      pricingTiers: [
        { minQuantity: 1, maxQuantity: null, unitPrice: 3.00 },
      ],
    },
  ],
  finishes: ['Lucioasă', 'Mată'],
};

const SINGLE_SIZE_PRODUCT: Product = {
  ...MOCK_PRODUCT,
  sizes: [MOCK_PRODUCT.sizes[0]],
};

function makeRoute(id: string) {
  return {
    snapshot: { paramMap: { get: () => id } },
  };
}

function setupModule(product: Product | null, id = 'p1') {
  const productService = {
    getProduct: vi.fn().mockReturnValue(
      product !== null ? of(product) : throwError(() => new Error('404')),
    ),
    getCatalog: vi.fn().mockReturnValue(of([])),
  };
  return { productService };
}

describe('FormatSelectorPage', () => {
  let fixture: ComponentFixture<FormatSelectorPage>;
  let component: FormatSelectorPage;

  async function setup(product: Product | null = MOCK_PRODUCT, id = 'p1', uploads: UploadState[] = []) {
    const { productService } = setupModule(product, id);
    await TestBed.configureTestingModule({
      imports: [FormatSelectorPage],
      providers: [
        provideRouter(TEST_ROUTES),
        { provide: ProductService, useValue: productService },
        { provide: ActivatedRoute, useValue: makeRoute(id) },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(FormatSelectorPage);
    component = fixture.componentInstance;
    component.uploads.set(uploads);
    fixture.detectChanges();
  }

  it('should create', async () => {
    await setup();
    expect(component).toBeTruthy();
  });

  it('shows error state when product not found', async () => {
    await setup(null);
    const errorEl = fixture.nativeElement.querySelector('.format-selector__error');
    expect(errorEl).not.toBeNull();
  });

  it('shows product name after load', async () => {
    await setup();
    const title = fixture.nativeElement.querySelector('.format-selector__title');
    expect(title?.textContent?.trim()).toBe('Poze foto');
  });

  it('form starts invalid (no size selected)', async () => {
    await setup();
    expect(component.form.invalid).toBe(true);
  });

  it('form becomes valid after selecting size and valid quantity', async () => {
    await setup();
    component.form.patchValue({ sizeId: 's1' });
    expect(component.form.valid).toBe(true);
  });

  it('canAddToCart is false when no size selected', async () => {
    await setup();
    expect(component.canAddToCart()).toBe(false);
  });

  it('canAddToCart is true after selecting size and having a done upload', async () => {
    const doneUpload: UploadState = { clientId: 'c1', file: new File([''], 'photo.jpg'), progress: 100, status: 'done', quantity: 1, dto: { id: 'u1', originalFileName: 'photo.jpg', contentType: 'image/jpeg', widthPx: 1200, heightPx: 1800, fileSizeBytes: 1024, uploadedAt: '' } };
    await setup(MOCK_PRODUCT, 'p1', [doneUpload]);
    component.form.patchValue({ sizeId: 's1' });
    component.selectedSize.set(MOCK_PRODUCT.sizes[0]);
    expect(component.canAddToCart()).toBe(true);
  });

  it('canAddToCart is false when size is selected but no uploads are done', async () => {
    await setup();
    component.selectedSize.set(MOCK_PRODUCT.sizes[0]);
    expect(component.canAddToCart()).toBe(false);
  });

  it('priceResult is null when no size selected', async () => {
    await setup();
    expect(component.priceResult()).toBeNull();
  });

  it('priceResult returns correct price for tier 1', async () => {
    const doneUpload: UploadState = { clientId: 'c1', file: new File([''], 'p.jpg'), progress: 100, status: 'done', quantity: 5 };
    await setup(MOCK_PRODUCT, 'p1', [doneUpload]);
    component.selectedSize.set(MOCK_PRODUCT.sizes[0]);
    const price = component.priceResult();
    expect(price?.unitPrice).toBe(2.50);
    expect(price?.totalPrice).toBe(12.50);
    expect(price?.tierLabel).toBe('1–9');
  });

  it('priceResult returns second tier price for qty=15', async () => {
    const doneUpload: UploadState = { clientId: 'c1', file: new File([''], 'p.jpg'), progress: 100, status: 'done', quantity: 15 };
    await setup(MOCK_PRODUCT, 'p1', [doneUpload]);
    component.selectedSize.set(MOCK_PRODUCT.sizes[0]);
    const price = component.priceResult();
    expect(price?.unitPrice).toBe(1.80);
    expect(price?.totalPrice).toBe(27.00);
    expect(price?.tierLabel).toBe('10+');
  });

  it('pre-selects the only size when product has one size', async () => {
    await setup(SINGLE_SIZE_PRODUCT);
    expect(component.form.get('sizeId')!.value).toBe('s1');
  });

  it('pre-selects the first finish', async () => {
    await setup();
    expect(component.form.get('finish')!.value).toBe('Lucioasă');
  });

  it('sets loading=false after successful load', async () => {
    await setup();
    expect(component.loading).toBe(false);
  });

  it('sets loading=false after error', async () => {
    await setup(null);
    expect(component.loading).toBe(false);
  });

  it('sets error message when product not found', async () => {
    await setup(null);
    expect(component.error).toBeTruthy();
  });
});

// ── Guest-session self-heal (bolt 042: FE-1, FE-2, FE-4) ──────────────────────
// These drive the component with mocked auth/upload services so the guest-session
// dedup and 401 retry paths can be exercised deterministically. ngOnInit is NOT run
// (no detectChanges), so each test invokes the target method directly.
describe('FormatSelectorPage — guest-session self-heal', () => {
  const DTO: UploadDto = {
    id: 'u1', originalFileName: 'p.jpg', contentType: 'image/jpeg',
    widthPx: 800, heightPx: 600, fileSizeBytes: 1024, uploadedAt: '',
  };

  function doneUpload(clientId: string): UploadState {
    return { clientId, progress: 100, status: 'done', quantity: 1, dto: { ...DTO } };
  }

  async function setupWithMocks(opts: {
    auth: Partial<AuthService>;
    guestAuth: Partial<GuestAuthService>;
    upload: Partial<UploadService>;
  }): Promise<FormatSelectorPage> {
    const productService = { getProduct: vi.fn().mockReturnValue(of(MOCK_PRODUCT)), getCatalog: vi.fn().mockReturnValue(of([])) };
    await TestBed.configureTestingModule({
      imports: [FormatSelectorPage],
      providers: [
        provideRouter(TEST_ROUTES),
        { provide: ProductService, useValue: productService },
        { provide: ActivatedRoute, useValue: makeRoute('p1') },
        { provide: AuthService, useValue: opts.auth },
        { provide: GuestAuthService, useValue: opts.guestAuth },
        { provide: UploadService, useValue: opts.upload },
        { provide: CartService, useValue: { snapshot: { groups: [] }, setCart: () => of(void 0) } },
      ],
    }).compileComponents();
    return TestBed.createComponent(FormatSelectorPage).componentInstance;
  }

  it('shares one in-flight anonymous-session init across concurrent callers (FE-1)', async () => {
    const init$ = new Subject<{ guestToken: string }>();
    const guestAuth = { initAnonymousSession: vi.fn(() => init$.asObservable()), storeSession: vi.fn() };
    const auth = { isAuthenticated: () => false, getGuestToken: () => null, clearGuestToken: vi.fn() };
    const c = await setupWithMocks({ auth, guestAuth, upload: {} });

    (c as unknown as { ensureGuestSession(): { subscribe(): void } }).ensureGuestSession().subscribe();
    (c as unknown as { ensureGuestSession(): { subscribe(): void } }).ensureGuestSession().subscribe();

    expect(guestAuth.initAnonymousSession).toHaveBeenCalledTimes(1);
  });

  it('re-inits a fresh session and retries the upload exactly once after a 401 (FE-2)', async () => {
    // Model the real flow: the first attempt goes out with a stale token, 401s, and the
    // errorInterceptor clears the token; on retry ensureGuestSession sees no token and
    // re-inits. Simulate the interceptor's clear by nulling the token on the 401 so the
    // re-init path is genuinely exercised (not just the retry wiring).
    let token: string | null = 'stale';
    const auth = { isAuthenticated: () => false, getGuestToken: () => token, clearGuestToken: vi.fn(() => { token = null; }) };
    const guestAuth = { initAnonymousSession: vi.fn(() => of({ guestToken: 'fresh' })), storeSession: vi.fn() };
    let attempts = 0;
    const upload = {
      upload: vi.fn(() => {
        attempts++;
        if (attempts === 1) {
          token = null; // interceptor clears the stale token on the 401
          return throwError(() => new HttpErrorResponse({ status: 401 }));
        }
        return of({ type: 'done' as const, dto: { ...DTO } });
      }),
    };
    const c = await setupWithMocks({ auth, guestAuth, upload });

    c.onFilesAccepted([new File(['x'], 'p.jpg')]);

    expect(upload.upload).toHaveBeenCalledTimes(2);
    expect(guestAuth.initAnonymousSession).toHaveBeenCalledTimes(1); // re-init actually happened
    expect(c.uploads()[0].status).toBe('done');
  });

  it('does not retry a non-401 upload failure (FE-2)', async () => {
    const auth = { isAuthenticated: () => false, getGuestToken: () => 'tok', clearGuestToken: vi.fn() };
    const guestAuth = { initAnonymousSession: vi.fn(() => of({ guestToken: 'fresh' })), storeSession: vi.fn() };
    const upload = { upload: vi.fn(() => throwError(() => new HttpErrorResponse({ status: 500 }))) };
    const c = await setupWithMocks({ auth, guestAuth, upload });

    c.onFilesAccepted([new File(['x'], 'p.jpg')]);

    expect(upload.upload).toHaveBeenCalledTimes(1);
    expect(c.uploads()[0].status).toBe('error');
  });

  it('re-inits and retries a restored preview once on 401, keeping the entry (FE-4)', async () => {
    const auth = { isAuthenticated: () => false, getGuestToken: () => null, clearGuestToken: vi.fn() };
    const guestAuth = { initAnonymousSession: vi.fn(() => of({ guestToken: 'fresh' })), storeSession: vi.fn() };
    let n = 0;
    const upload = {
      getPreviewBlob: vi.fn(() => {
        n++;
        return n === 1 ? throwError(() => new HttpErrorResponse({ status: 401 })) : of('blob:x');
      }),
    };
    const c = await setupWithMocks({ auth, guestAuth, upload });
    c.uploads.set([doneUpload('c1')]);

    (c as unknown as { fetchPreviewWithRetry(id: string, cid: string, r: boolean): void })
      .fetchPreviewWithRetry('u1', 'c1', false);

    expect(upload.getPreviewBlob).toHaveBeenCalledTimes(2);
    expect(c.uploads().find(u => u.clientId === 'c1')?.previewUrl).toBe('blob:x');
  });

  it('drops a restored entry on a 404 without re-init (FE-4)', async () => {
    const auth = { isAuthenticated: () => false, getGuestToken: () => null, clearGuestToken: vi.fn() };
    const guestAuth = { initAnonymousSession: vi.fn(() => of({ guestToken: 'fresh' })), storeSession: vi.fn() };
    const upload = { getPreviewBlob: vi.fn(() => throwError(() => new HttpErrorResponse({ status: 404 }))) };
    const c = await setupWithMocks({ auth, guestAuth, upload });
    c.uploads.set([doneUpload('c1')]);

    (c as unknown as { fetchPreviewWithRetry(id: string, cid: string, r: boolean): void })
      .fetchPreviewWithRetry('u1', 'c1', false);

    expect(c.uploads().find(u => u.clientId === 'c1')).toBeUndefined();
    expect(guestAuth.initAnonymousSession).not.toHaveBeenCalled();
  });
});
