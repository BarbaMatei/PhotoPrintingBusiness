import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Routes, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FormatSelectorPage } from './format-selector-page';
import { ProductService } from '../../../../core/services/product.service';
import { Product } from '../../../../core/models/product.model';
import { UploadState } from '../../../../core/models/upload.model';

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
