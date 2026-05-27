import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { AdminOrdersPage } from './admin-orders-page';
import { AdminHubService } from '../../../../core/services/admin-hub.service';
import { environment } from '../../../../../environments/environment';
import { Subject } from 'rxjs';
import type { NewOrderEvent } from '../../../../core/models/admin.model';

const base = `${environment.apiUrl}/admin`;

class FakeAdminHubService {
  newOrderReceived$ = new Subject<NewOrderEvent>();
  orderStatusChanged$ = new Subject<{ orderId: string; status: string }>();
  connect = vi.fn().mockResolvedValue(undefined);
}

describe('AdminOrdersPage', () => {
  let fixture: ComponentFixture<AdminOrdersPage>;
  let component: AdminOrdersPage;
  let http: HttpTestingController;
  let hubService: FakeAdminHubService;

  beforeEach(async () => {
    hubService = new FakeAdminHubService();

    await TestBed.configureTestingModule({
      imports: [AdminOrdersPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AdminHubService, useValue: hubService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminOrdersPage);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function flushOrders(items = [{ id: 'o1', orderNumber: 'FT-001', status: 'Paid', customerEmail: 'a@b.com', customerName: 'Ion', totalRon: 50, createdAt: new Date().toISOString(), itemCount: 2, deliveryType: 'Courier' }], total = 1) {
    const req = http.expectOne(r => r.url.includes(`${base}/orders`));
    req.flush({ items, total });
  }

  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('loads orders on init', () => {
    fixture.detectChanges();
    flushOrders();
    fixture.detectChanges();

    expect(component.orders.length).toBe(1);
    expect(component.total).toBe(1);
    expect(component.loading).toBe(false);
  });

  it('sends page param on load', () => {
    fixture.detectChanges();

    const req = http.expectOne(r => r.url.includes(`${base}/orders`));
    expect(req.request.params.get('page')).toBe('1');
    req.flush({ items: [], total: 0 });
  });

  it('prepends new order from SignalR event', () => {
    fixture.detectChanges();
    flushOrders([], 0);
    fixture.detectChanges();

    hubService.newOrderReceived$.next({
      id: 'new-id',
      orderNumber: 'FT-999',
      customerEmail: 'new@test.ro',
      customerName: 'New',
      totalRon: 80,
      createdAt: new Date().toISOString(),
      status: 'Paid',
    });
    fixture.detectChanges();

    expect(component.orders[0].orderNumber).toBe('FT-999');
    expect(component.total).toBe(1);
  });

  it('updates order status from SignalR event', () => {
    fixture.detectChanges();
    flushOrders([{ id: 'o1', orderNumber: 'FT-001', status: 'Paid', customerEmail: 'a@b.com', customerName: 'Ion', totalRon: 50, createdAt: new Date().toISOString(), itemCount: 2, deliveryType: 'Courier' }], 1);
    fixture.detectChanges();

    hubService.orderStatusChanged$.next({ orderId: 'o1', status: 'Printing' });
    fixture.detectChanges();

    expect(component.orders[0].status).toBe('Printing');
  });

  it('totalPages is computed correctly', () => {
    component.total = 45;
    component.pageSize = 20;

    expect(component.totalPages).toBe(3);
  });

  it('nextPage increments page and reloads', () => {
    component.total = 50;
    component.pageSize = 20;
    component.page = 1;
    component.orders = [];
    fixture.detectChanges();

    // Consume initial load
    flushOrders([], 50);

    component.nextPage();

    expect(component.page).toBe(2);
    flushOrders([], 50); // second load
  });

  it('prevPage does not go below 1', () => {
    component.page = 1;
    component.prevPage();

    expect(component.page).toBe(1);
  });
});
