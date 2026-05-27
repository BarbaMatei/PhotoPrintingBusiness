import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { of, BehaviorSubject } from 'rxjs';
import { CartService } from './cart.service';
import { AuthService } from './auth.service';
import { CartResponseDto, EMPTY_CART, CART_STORAGE_KEY } from '../models/cart.model';
import { environment } from '../../../environments/environment';

function makeCart(itemCount: number): CartResponseDto {
  return {
    groups: [
      {
        productId: '11111111-1111-1111-1111-111111111111',
        productName: 'Foto 10×15',
        sizeId: 'size-1',
        sizeName: '10×15',
        finishName: 'Lucios',
        items: Array.from({ length: itemCount }, (_, i) => ({
          uploadId: `upload-${i}`,
          quantity: 1,
          previewUrl: `/api/uploads/upload-${i}/preview`,
          unitPrice: 2,
          lineTotal: 2,
          widthPx: 1200,
          heightPx: 1800,
        })),
        totalCopies: itemCount,
        unitPrice: 2,
        subtotal: itemCount * 2,
      },
    ],
    subtotal: itemCount * 2,
    itemCount,
  };
}

const BASE = `${environment.apiUrl}/cart`;

describe('CartService', () => {
  let service: CartService;
  let http: HttpTestingController;
  let isAuthSubject: BehaviorSubject<boolean>;

  beforeEach(() => {
    isAuthSubject = new BehaviorSubject<boolean>(false);
    localStorage.removeItem(CART_STORAGE_KEY);

    const mockAuth = {
      isAuthenticated$: isAuthSubject.asObservable(),
      isAuthenticated: () => isAuthSubject.value,
      getAccessToken: () => null,
      getGuestToken: () => 'guest-token-123',
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuth },
      ],
    });

    service = TestBed.inject(CartService);
    http = TestBed.inject(HttpTestingController);

    // Flush any initial GET triggered by isAuthenticated$ for guests (no request when guest with empty localStorage)
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(CART_STORAGE_KEY);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('starts with itemCount of 0', () => {
    expect(service.itemCount()).toBe(0);
  });

  it('itemCount$ emits 0 on subscribe', () => {
    const values: number[] = [];
    service.itemCount$.subscribe(v => values.push(v));
    expect(values).toEqual([0]);
  });

  it('setCart updates itemCount$ after server response', () => {
    const values: number[] = [];
    service.itemCount$.subscribe(v => values.push(v));

    const cart = makeCart(3);
    service.setCart({ productId: 'p1', sizeId: 's1', finishName: null, items: [{ uploadId: 'u1', quantity: 3 }] }).subscribe();

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('POST');
    req.flush(cart);

    expect(values).toContain(3);
  });

  it('setCart persists cart to localStorage for guest user', () => {
    const cart = makeCart(2);
    service.setCart({ productId: 'p1', sizeId: 's1', finishName: null, items: [{ uploadId: 'u1', quantity: 2 }] }).subscribe();

    const req = http.expectOne(BASE);
    req.flush(cart);

    const stored = localStorage.getItem(CART_STORAGE_KEY);
    expect(stored).not.toBeNull();
    const parsed = JSON.parse(stored!) as CartResponseDto;
    expect(parsed.itemCount).toBe(2);
  });

  it('setCart does NOT persist to localStorage when user is authenticated', () => {
    isAuthSubject.next(true);

    // Flush the GET from loadFromServer triggered by auth state change
    const getReq = http.expectOne(BASE);
    getReq.flush(EMPTY_CART);

    const cart = makeCart(2);
    service.setCart({ productId: 'p1', sizeId: 's1', finishName: null, items: [{ uploadId: 'u1', quantity: 2 }] }).subscribe();

    const postReq = http.expectOne(BASE);
    postReq.flush(cart);

    expect(localStorage.getItem(CART_STORAGE_KEY)).toBeNull();
  });

  it('clearCart resets itemCount$ to 0 and removes localStorage entry', () => {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(makeCart(2)));
    const values: number[] = [];
    service.itemCount$.subscribe(v => values.push(v));

    service.clearCart().subscribe();

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(values).toContain(0);
    expect(localStorage.getItem(CART_STORAGE_KEY)).toBeNull();
  });

  it('mergeOnLogin POSTs to /cart/merge and clears localStorage', () => {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(makeCart(2)));
    const merged = makeCart(5);

    service.mergeOnLogin('guest-session-abc').subscribe();

    const req = http.expectOne(`${BASE}/merge`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ guestSessionId: 'guest-session-abc' });
    req.flush(merged);

    expect(service.itemCount()).toBe(5);
    expect(localStorage.getItem(CART_STORAGE_KEY)).toBeNull();
  });

  it('loadFromLocalStorage restores cart on construction for guest', () => {
    // Rebuild service with localStorage pre-populated
    const savedCart = makeCart(4);
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(savedCart));

    // Re-create service
    TestBed.resetTestingModule();
    isAuthSubject = new BehaviorSubject<boolean>(false);
    const mockAuth = {
      isAuthenticated$: isAuthSubject.asObservable(),
      isAuthenticated: () => false,
      getAccessToken: () => null,
      getGuestToken: () => 'guest',
    };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuth },
      ],
    });
    const freshService = TestBed.inject(CartService);
    TestBed.inject(HttpTestingController).verify(); // No HTTP requests expected

    expect(freshService.itemCount()).toBe(4);
  });
});

