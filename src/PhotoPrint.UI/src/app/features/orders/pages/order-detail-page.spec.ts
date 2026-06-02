import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { OrderDetailPage } from './order-detail-page';
import { OrderService } from '../../../core/services/order.service';
import { OrderDetailDto } from '../../../core/models/order.model';

const MOCK_DETAIL: OrderDetailDto = {
  id: 'order-1',
  orderNumber: 'FT-001',
  status: 'Paid',
  totalRon: 120,
  subtotalRon: 100,
  shippingCostRon: 20,
  createdAt: '2026-05-01T12:00:00Z',
  paidAt: '2026-05-01T12:05:00Z',
  deliveryType: 'Easybox',
  itemCount: 1,
  paymentProcessor: 'Stripe',
  lockerId: 'locker-1',
  lockerName: 'Easybox Mega Mall',
  lockerAddress: 'Str. Exemplu 1, București',
  shippingAddress: null,
  items: [
    {
      uploadId: 'upload-1',
      previewUrl: '/api/uploads/upload-1/preview',
      productName: 'Fotografie 10×15',
      size: '10x15',
      finish: 'Lucioasă',
      quantity: 2,
      unitPriceRon: 50,
      lineTotalRon: 100,
    },
  ],
};

function makeOrderService(overrides: Partial<OrderService> = {}): Partial<OrderService> {
  return {
    getOrderDetail: vi.fn().mockReturnValue(of(MOCK_DETAIL)),
    // Bolt 053: photos endpoint — default empty so legacy tests don't need to know about it.
    getOrderPhotos: vi.fn().mockReturnValue(of({ photos: [] })),
    ...overrides,
  };
}

describe('OrderDetailPage', () => {
  let fixture: ComponentFixture<OrderDetailPage>;
  let component: OrderDetailPage;
  let router: Router;

  async function setup(serviceOverrides: Partial<OrderService> = {}, navigateSpy?: ReturnType<typeof vi.spyOn>) {
    await TestBed.configureTestingModule({
      imports: [OrderDetailPage],
      providers: [
        provideRouter([]),
        { provide: OrderService, useValue: makeOrderService(serviceOverrides) },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    // If a spy was passed in, it was already installed; otherwise leave navigate unspied
    if (navigateSpy === undefined && !serviceOverrides.getOrderDetail) {
      // normal happy-path setup
    }
    fixture = TestBed.createComponent(OrderDetailPage);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();
  }

  async function setupWithErrorAndNavigateSpy(status: number) {
    await TestBed.configureTestingModule({
      imports: [OrderDetailPage],
      providers: [
        provideRouter([]),
        {
          provide: OrderService,
          useValue: makeOrderService({
            getOrderDetail: vi.fn().mockReturnValue(throwError(() => ({ status }))),
          }),
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    const spy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(OrderDetailPage);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();
    return spy;
  }

  it('renders order number after loading', async () => {
    await setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('FT-001');
  });

  it('renders line item product name', async () => {
    await setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Fotografie 10×15');
  });

  it('renders subtotal, shipping and total amounts', async () => {
    await setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('100.00 RON');
    expect(el.textContent).toContain('20.00 RON');
    expect(el.textContent).toContain('120.00 RON');
  });

  it('shows locker name for Easybox orders', async () => {
    await setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Easybox Mega Mall');
  });

  it('shows shipping address for Courier orders', async () => {
    const courierDetail: OrderDetailDto = {
      ...MOCK_DETAIL,
      deliveryType: 'Courier',
      lockerId: null,
      lockerName: null,
      lockerAddress: null,
      shippingAddress: {
        recipientName: 'Ion Popescu',
        street: 'Str. Florilor',
        number: '10',
        block: null,
        city: 'Cluj-Napoca',
        county: 'Cluj',
        postalCode: '400001',
        phone: '0712345678',
      },
    };
    await setup({
      getOrderDetail: vi.fn().mockReturnValue(of(courierDetail)),
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Ion Popescu');
    expect(el.textContent).toContain('Cluj-Napoca');
  });

  it('navigates to /comenzile-mele on 403', async () => {
    const navigateSpy = await setupWithErrorAndNavigateSpy(403);
    expect(navigateSpy).toHaveBeenCalledWith(['/comenzile-mele']);
  });

  it('navigates to /comenzile-mele on 404', async () => {
    const navigateSpy = await setupWithErrorAndNavigateSpy(404);
    expect(navigateSpy).toHaveBeenCalledWith(['/comenzile-mele']);
  });

  // ── Bolt 053: photo archive grid + lightbox ─────────────────────────────

  it('shows the "no longer available" copy when the photos endpoint returns empty', async () => {
    await setup({
      getOrderPhotos: vi.fn().mockReturnValue(of({ photos: [] })),
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Fotografiile pentru această comandă nu mai sunt disponibile');
  });

  it('renders a thumbnail tile per photo returned by the endpoint', async () => {
    await setup({
      getOrderPhotos: vi.fn().mockReturnValue(of({
        photos: [
          { uploadId: 'u1', fileName: 'sunset.jpg', thumbnailUrl: 'https://cdn.test/t1', largeUrl: 'https://cdn.test/l1' },
          { uploadId: 'u2', fileName: 'beach.jpg',  thumbnailUrl: 'https://cdn.test/t2', largeUrl: 'https://cdn.test/l2' },
        ],
      })),
    });
    const tiles = (fixture.nativeElement as HTMLElement).querySelectorAll('.photo-tile');
    expect(tiles.length).toBe(2);
    const imgs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLImageElement>('.photo-tile img'));
    expect(imgs[0].getAttribute('src')).toBe('https://cdn.test/t1');
    expect(imgs[1].getAttribute('src')).toBe('https://cdn.test/t2');
  });

  it('uses native lazy-loading on thumbnail images', async () => {
    await setup({
      getOrderPhotos: vi.fn().mockReturnValue(of({
        photos: [
          { uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 't1', largeUrl: 'l1' },
        ],
      })),
    });
    const img = (fixture.nativeElement as HTMLElement).querySelector<HTMLImageElement>('.photo-tile img')!;
    expect(img.getAttribute('loading')).toBe('lazy');
  });

  it('does not render the lightbox until a thumbnail is clicked', async () => {
    await setup({
      getOrderPhotos: vi.fn().mockReturnValue(of({
        photos: [
          { uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 't1', largeUrl: 'l1' },
        ],
      })),
    });
    // The lightbox renders nothing while [src] is null — its template only emits when src truthy.
    expect((fixture.nativeElement as HTMLElement).querySelector('.lightbox__backdrop')).toBeNull();
  });

  it('opens the lightbox with the largeUrl when a thumbnail is clicked', async () => {
    await setup({
      getOrderPhotos: vi.fn().mockReturnValue(of({
        photos: [
          { uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 't1', largeUrl: 'https://cdn.test/large-1' },
        ],
      })),
    });

    const tile = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.photo-tile')!;
    tile.click();
    fixture.detectChanges();

    const img = (fixture.nativeElement as HTMLElement).querySelector<HTMLImageElement>('.lightbox__img')!;
    expect(img).not.toBeNull();
    expect(img.getAttribute('src')).toBe('https://cdn.test/large-1');
  });

  it('closes the lightbox when its close event fires', async () => {
    await setup({
      getOrderPhotos: vi.fn().mockReturnValue(of({
        photos: [
          { uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 't1', largeUrl: 'l1' },
        ],
      })),
    });

    // Open then close — the backdrop click handler emits (close).
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.photo-tile')!.click();
    fixture.detectChanges();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.lightbox__backdrop')!.click();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.lightbox__backdrop')).toBeNull();
  });

  it('silently empties the photos list when the photos call fails (no navigation)', async () => {
    // A photos-endpoint failure must NOT redirect the page — the customer still sees their
    // order detail, with the "no longer available" copy in the photos section.
    await TestBed.configureTestingModule({
      imports: [OrderDetailPage],
      providers: [
        provideRouter([]),
        {
          provide: OrderService,
          useValue: makeOrderService({
            getOrderPhotos: vi.fn().mockReturnValue(throwError(() => ({ status: 500 }))),
          }),
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(OrderDetailPage);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();

    expect(navigateSpy).not.toHaveBeenCalled();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Fotografiile pentru această comandă nu mai sunt disponibile');
  });
});
