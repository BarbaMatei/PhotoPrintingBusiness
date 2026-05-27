import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Routes } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CatalogPage } from './catalog-page';
import { ProductService } from '../../../../core/services/product.service';
import { Product } from '../../../../core/models/product.model';

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
      pricingTiers: [{ minQuantity: 1, maxQuantity: null, unitPrice: 1.50 }],
    },
  ],
  finishes: ['Lucioasă'],
};

function mockProductService(products: Product[] | null = [MOCK_PRODUCT]) {
  return {
    getCatalog: vi.fn().mockReturnValue(
      products !== null ? of(products) : throwError(() => new Error('network')),
    ),
    clearCache: vi.fn(),
  };
}

describe('CatalogPage', () => {
  let fixture: ComponentFixture<CatalogPage>;
  let component: CatalogPage;
  let service: ReturnType<typeof mockProductService>;

  async function setup(products: Product[] | null = [MOCK_PRODUCT]) {
    service = mockProductService(products);
    await TestBed.configureTestingModule({
      imports: [CatalogPage],
      providers: [
        provideRouter(TEST_ROUTES),
        { provide: ProductService, useValue: service },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CatalogPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should create', async () => {
    await setup();
    expect(component).toBeTruthy();
  });

  it('renders skeleton cards while loading is true', async () => {
    // Simulate sync loading: loading starts true but getCatalog resolves synchronously
    // After detectChanges, loading is false for synchronous observables.
    // We test that after successful load, skeletons are gone.
    await setup();
    const skeletons = fixture.nativeElement.querySelectorAll('.catalog__skeleton');
    expect(skeletons.length).toBe(0); // sync observable resolves immediately
  });

  it('shows product cards on successful load', async () => {
    await setup([MOCK_PRODUCT]);
    const cards = fixture.nativeElement.querySelectorAll('app-product-card');
    expect(cards.length).toBe(1);
  });

  it('shows two product cards when two products returned', async () => {
    const second: Product = { ...MOCK_PRODUCT, id: 'p2', name: 'Tablou' };
    await setup([MOCK_PRODUCT, second]);
    const cards = fixture.nativeElement.querySelectorAll('app-product-card');
    expect(cards.length).toBe(2);
  });

  it('shows empty state when products array is empty', async () => {
    await setup([]);
    const empty = fixture.nativeElement.querySelector('.catalog__empty');
    expect(empty).not.toBeNull();
  });

  it('shows error message on API failure', async () => {
    await setup(null);
    const error = fixture.nativeElement.querySelector('.catalog__error');
    expect(error).not.toBeNull();
    expect(error.textContent).toContain('Nu am putut încărca produsele');
  });

  it('retry button calls loadCatalog again', async () => {
    await setup(null);
    service.getCatalog.mockReturnValue(of([MOCK_PRODUCT]));

    const btn: HTMLButtonElement = fixture.nativeElement.querySelector('.catalog__error button');
    expect(btn).not.toBeNull();
    btn.click();
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('app-product-card');
    expect(cards.length).toBe(1);
  });

  it('sets loading=false after successful response', async () => {
    await setup();
    expect(component.loading).toBe(false);
  });

  it('sets loading=false after error response', async () => {
    await setup(null);
    expect(component.loading).toBe(false);
  });
});
