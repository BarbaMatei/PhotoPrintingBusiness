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
  netTotalRon: 100.84,
  vatRon: 19.16,
  vatRate: 0.19,
  shippingCostRon: 20,
  couponCode: null,
  discountRon: 0,
  createdAt: '2026-05-01T12:00:00Z',
  paidAt: '2026-05-01T12:05:00Z',
  deliveryType: 'Easybox',
  itemCount: 1,
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
    // Photos endpoint — default empty so legacy tests don't need to know about it.
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

  // The customer is invoiced VAT-inclusive, so the amount has to be visible somewhere.
  it('renders the TVA line with the rate the server sent', async () => {
    await setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('TVA');
    expect(el.textContent).toContain('19.16 RON');
    expect(el.textContent).toContain('19%');
  });

  it('renders the coupon discount row, so the receipt explains why the total is lower', async () => {
    await setup({
      getOrderDetail: vi.fn().mockReturnValue(of({
        ...MOCK_DETAIL,
        couponCode: 'VARA10',
        discountRon: 25,
        totalRon: 95,
      })),
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Reducere (VARA10)');
    expect(el.textContent).toContain('-25.00 RON');
  });

  it('omits the discount row for an order without a coupon', async () => {
    await setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Reducere');
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

  // An empty archive only means "purged" once the order could have been purged (shipped /
  // delivered / cancelled). Before that, photos are still being prepared.
  for (const status of ['AwaitingPayment', 'Pending', 'Paid', 'Printing'] as const) {
    it(`shows the "available soon" copy for an empty archive on a ${status} order`, async () => {
      await setup({
        getOrderDetail: vi.fn().mockReturnValue(of({ ...MOCK_DETAIL, status: status as OrderDetailDto['status'] })),
        getOrderPhotos: vi.fn().mockReturnValue(of({ photos: [] })),
      });
      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('vor fi disponibile în curând');
      expect(el.textContent).not.toContain('nu mai sunt disponibile');
    });
  }

  for (const status of ['Shipped', 'Delivered', 'Cancelled', 'PaymentFailed'] as const) {
    it(`keeps the "no longer available" copy for an empty archive on a ${status} order`, async () => {
      await setup({
        getOrderDetail: vi.fn().mockReturnValue(of({ ...MOCK_DETAIL, status: status as OrderDetailDto['status'] })),
        getOrderPhotos: vi.fn().mockReturnValue(of({ photos: [] })),
      });
      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Fotografiile pentru această comandă nu mai sunt disponibile');
      expect(el.textContent).not.toContain('vor fi disponibile în curând');
    });
  }

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

  it('shows a photos error + retry (NOT "no longer available") when the photos call fails, without navigating', async () => {
    // A photos-endpoint FAILURE must be distinguished from a genuine empty
    // result. The old code mapped any error to [] and showed the permanent "no longer available"
    // copy. It must now show a retryable error, and still not redirect the page.
    await setup({
      getOrderPhotos: vi.fn().mockReturnValue(throwError(() => ({ status: 500 }))),
    });
    router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Fotografiile nu au putut fi încărcate');
    expect(el.textContent).not.toContain('nu mai sunt disponibile');
    expect(el.querySelector('.photos-retry')).not.toBeNull();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('retries the photos fetch when the retry button is clicked (F6/D13)', async () => {
    const getOrderPhotos = vi
      .fn()
      .mockReturnValueOnce(throwError(() => ({ status: 500 })))
      .mockReturnValueOnce(of({
        photos: [{ uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 't1', largeUrl: 'l1' }],
      }));
    await setup({ getOrderPhotos });

    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLButtonElement>('.photos-retry')!.click();
    fixture.detectChanges();

    expect(getOrderPhotos).toHaveBeenCalledTimes(2);
    expect(el.querySelectorAll('.photo-tile').length).toBe(1);
    expect(el.querySelector('.photos-retry')).toBeNull();
  });

  it('shows an inline order error + retry (no redirect) on a transient 500 (F16/D32)', async () => {
    // The strand-a-logged-out-user Medium was refuted; the residual is that a transient/5xx error
    // must NOT bounce the user to the orders list with no retry.
    const navigateSpy = await setupWithErrorAndNavigateSpy(500);

    const el = fixture.nativeElement as HTMLElement;
    expect(navigateSpy).not.toHaveBeenCalled();
    expect(el.querySelector('.order-error')).not.toBeNull();
    expect(el.textContent).toContain('Comanda nu a putut fi încărcată');
  });

  it('does NOT navigate on a 401 — leaves it to the auth interceptor (F16/D32)', async () => {
    const navigateSpy = await setupWithErrorAndNavigateSpy(401);
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('refreshes an expired lightbox URL on image error and re-points the lightbox (F7/D5b)', async () => {
    const getOrderPhotos = vi
      .fn()
      .mockReturnValueOnce(of({
        photos: [{ uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 't1', largeUrl: 'https://cdn/stale' }],
      }))
      .mockReturnValueOnce(of({
        photos: [{ uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 't1', largeUrl: 'https://cdn/fresh' }],
      }));
    await setup({ getOrderPhotos });

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.photo-tile')!.click();
    fixture.detectChanges();
    expect(component.lightboxSrc()).toBe('https://cdn/stale');

    // The <img> fails to load the expired URL → the lightbox emits (imgError).
    (fixture.nativeElement as HTMLElement).querySelector<HTMLImageElement>('.lightbox__img')!
      .dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(getOrderPhotos).toHaveBeenCalledTimes(2);
    expect(component.lightboxSrc()).toBe('https://cdn/fresh');
  });

  it('does NOT re-open a closed lightbox when a grid thumbnail errors after close (D36 regression)', async () => {
    // Close cleared lightboxSrc but not lightboxPhotoId, so a later grid
    // thumbnail (error) → refreshPhotoUrls re-pointed the lightbox from the stale id and the closed
    // modal spontaneously re-opened. A closed lightbox must stay closed through a URL refresh.
    const getOrderPhotos = vi
      .fn()
      .mockReturnValueOnce(of({
        photos: [{ uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 'https://cdn/stale-t', largeUrl: 'https://cdn/stale-l' }],
      }))
      .mockReturnValueOnce(of({
        photos: [{ uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 'https://cdn/fresh-t', largeUrl: 'https://cdn/fresh-l' }],
      }));
    await setup({ getOrderPhotos });

    const el = fixture.nativeElement as HTMLElement;

    // Open the lightbox, then close it.
    el.querySelector<HTMLButtonElement>('.photo-tile')!.click();
    fixture.detectChanges();
    expect(component.lightboxSrc()).toBe('https://cdn/stale-l');
    el.querySelector<HTMLElement>('.lightbox__backdrop')!.click();
    fixture.detectChanges();
    expect(component.lightboxSrc()).toBeNull();

    // A stale grid thumbnail now errors → refreshPhotoUrls runs. The lightbox must NOT re-open.
    el.querySelector<HTMLImageElement>('.photo-tile img')!.dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(getOrderPhotos).toHaveBeenCalledTimes(2); // refresh did run (grid URLs updated)
    expect(component.lightboxSrc()).toBeNull(); // but the closed lightbox stayed closed
    expect(el.querySelector('.lightbox__backdrop')).toBeNull();
  });

  it('refreshes photo URLs when a GRID thumbnail image errors (F7/D5b class-sweep)', async () => {
    const getOrderPhotos = vi
      .fn()
      .mockReturnValueOnce(of({
        photos: [{ uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 'https://cdn/stale-t', largeUrl: 'l1' }],
      }))
      .mockReturnValueOnce(of({
        photos: [{ uploadId: 'u1', fileName: 'a.jpg', thumbnailUrl: 'https://cdn/fresh-t', largeUrl: 'l1' }],
      }));
    await setup({ getOrderPhotos });

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLImageElement>('.photo-tile img')!
      .dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(getOrderPhotos).toHaveBeenCalledTimes(2);
    expect(
      (fixture.nativeElement as HTMLElement).querySelector<HTMLImageElement>('.photo-tile img')!.getAttribute('src'),
    ).toBe('https://cdn/fresh-t');
  });
});
