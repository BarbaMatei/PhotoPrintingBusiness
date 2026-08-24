import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { AdminOrderDetailPage } from './admin-order-detail-page';
import { AdminHubService } from '../../../../core/services/admin-hub.service';
import { environment } from '../../../../../environments/environment';
import { Subject } from 'rxjs';
import type { AdminOrderDetailDto } from '../../../../core/models/admin.model';

const base = `${environment.apiUrl}/admin`;

class FakeAdminHubService {
  newOrderReceived$ = new Subject<never>();
  orderStatusChanged$ = new Subject<{ orderId: string; status: string }>();
  connect = vi.fn().mockResolvedValue(undefined);
}

const MOCK_ORDER: AdminOrderDetailDto = {
  id: 'order-123',
  orderNumber: 'FT-001',
  status: 'Paid',
  customerEmail: 'ion@test.ro',
  customerName: 'Ion Popescu',
  subtotalRon: 30,
  shippingCostRon: 15,
  totalRon: 45,
  createdAt: '2026-05-22T10:00:00Z',
  paidAt: '2026-05-22T10:05:00Z',
  deliveryType: 'Courier',
  lockerName: null,
  lockerAddress: null,
  shippingAddress: {
    recipientName: 'Ion',
    street: 'Str. Test',
    number: '1',
    block: null,
    city: 'București',
    county: 'Ilfov',
    postalCode: '010000',
    phone: '0700000000',
  },
  paymentIntentId: 'pi_test',
  awbNumber: null,
  trackingUrl: null,
  internalNotes: null,
  items: [
    {
      uploadId: 'upload-1',
      productName: 'Foto 10x15',
      size: '10x15',
      finish: 'Lucios',
      quantity: 10,
      unitPriceRon: 3,
      lineTotalRon: 30,
    },
  ],
};

describe('AdminOrderDetailPage', () => {
  let fixture: ComponentFixture<AdminOrderDetailPage>;
  let component: AdminOrderDetailPage;
  let http: HttpTestingController;
  let hubService: FakeAdminHubService;

  beforeEach(async () => {
    hubService = new FakeAdminHubService();

    await TestBed.configureTestingModule({
      imports: [AdminOrderDetailPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AdminHubService, useValue: hubService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminOrderDetailPage);
    component = fixture.componentInstance;
    // Set required input via component ref
    TestBed.runInInjectionContext(() => {});
    (component as any).orderId = () => 'order-123';
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function flushOrder(order = MOCK_ORDER) {
    const req = http.expectOne(`${base}/orders/order-123`);
    req.flush(order);
  }

  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('loads order detail on init', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    expect(component.order?.orderNumber).toBe('FT-001');
    expect(component.loading).toBe(false);
  });

  it('populates notesText from order.internalNotes', () => {
    fixture.detectChanges();
    flushOrder({ ...MOCK_ORDER, internalNotes: 'Handle with care' });
    fixture.detectChanges();

    expect(component.notesText).toBe('Handle with care');
  });

  it('nextStatuses for Paid includes Printing and Cancelled', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    expect(component.nextStatuses).toContain('Printing');
    expect(component.nextStatuses).toContain('Cancelled');
  });

  it('nextStatuses is empty for Delivered', () => {
    fixture.detectChanges();
    flushOrder({ ...MOCK_ORDER, status: 'Delivered' });
    fixture.detectChanges();

    expect(component.nextStatuses.length).toBe(0);
  });

  it('showAwbFields is true when selectedStatus is Shipped', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    component.selectedStatus = 'Shipped';
    expect(component.showAwbFields).toBe(true);
  });

  it('applyStatusChange calls updateOrderStatus and updates order', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    component.selectedStatus = 'Printing';
    component.applyStatusChange();

    const req = http.expectOne(`${base}/orders/order-123/status`);
    expect(req.request.body.status).toBe('Printing');
    req.flush({ ...MOCK_ORDER, status: 'Printing' });
    fixture.detectChanges();

    expect(component.order?.status).toBe('Printing');
    expect(component.selectedStatus).toBe('');
  });

  it('doCancel calls cancelOrder and updates order to Cancelled', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    component.doCancel();

    const req = http.expectOne(`${base}/orders/order-123/cancel`);
    expect(req.request.method).toBe('POST');
    req.flush({ ...MOCK_ORDER, status: 'Cancelled' });
    fixture.detectChanges();

    expect(component.order?.status).toBe('Cancelled');
    expect(component.showCancelModal).toBe(false);
  });

  it('saveNotes calls updateOrderNotes with notesText', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    component.notesText = 'Urgent';
    component.saveNotes();

    const req = http.expectOne(`${base}/orders/order-123/notes`);
    expect(req.request.body).toEqual({ notes: 'Urgent' });
    req.flush({ ...MOCK_ORDER, internalNotes: 'Urgent' });
    fixture.detectChanges();

    expect(component.order?.internalNotes).toBe('Urgent');
  });

  it('updates status from SignalR event', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    hubService.orderStatusChanged$.next({ orderId: 'order-123', status: 'Printing' });
    fixture.detectChanges();

    expect(component.order?.status).toBe('Printing');
  });

  it('ignores SignalR events for other orders', () => {
    fixture.detectChanges();
    flushOrder();
    fixture.detectChanges();

    hubService.orderStatusChanged$.next({ orderId: 'other-order', status: 'Cancelled' });
    fixture.detectChanges();

    expect(component.order?.status).toBe('Paid');
  });
});
