import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { AdminPage } from './admin-page';
import { AdminHubService } from '../../../core/services/admin-hub.service';
import { environment } from '../../../../environments/environment';
import { Subject } from 'rxjs';
import type { NewOrderEvent } from '../../../core/models/admin.model';

const base = `${environment.apiUrl}/admin`;

// Stub that avoids real SignalR connection
class FakeAdminHubService {
  newOrderReceived$ = new Subject<NewOrderEvent>();
  orderStatusChanged$ = new Subject<{ orderId: string; status: string }>();
  connect = vi.fn().mockResolvedValue(undefined);
  disconnect = vi.fn().mockResolvedValue(undefined);
}

describe('AdminPage', () => {
  let fixture: ComponentFixture<AdminPage>;
  let component: AdminPage;
  let http: HttpTestingController;
  let hubService: FakeAdminHubService;

  beforeEach(async () => {
    hubService = new FakeAdminHubService();

    await TestBed.configureTestingModule({
      imports: [AdminPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AdminHubService, useValue: hubService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminPage);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function flushStats() {
    http.expectOne(`${base}/stats/summary`).flush({
      todayOrders: 5, todayRevenue: 200, monthOrders: 100, monthRevenue: 4000,
    });
    http.expectOne(r => r.url.includes('/stats/revenue')).flush([]);
    http.expectOne(`${base}/stats/orders-by-status`).flush([]);
    http.expectOne(`${base}/stats/products`).flush([]);
  }

  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('connects to SignalR hub on init', () => {
    fixture.detectChanges();
    flushStats();

    expect(hubService.connect).toHaveBeenCalledOnce();
  });

  it('loads stats on init', () => {
    fixture.detectChanges();
    flushStats();
    fixture.detectChanges();

    expect(component.stats?.todayOrders).toBe(5);
    expect(component.stats?.monthRevenue).toBe(4000);
  });

  it('sets loading=false after data loads', () => {
    fixture.detectChanges();
    flushStats();
    fixture.detectChanges();

    expect(component.loading).toBe(false);
  });

  it('reloads stats when new order event arrives', () => {
    fixture.detectChanges();
    flushStats();

    hubService.newOrderReceived$.next({
      id: 'new-1',
      orderNumber: 'FT-NEW',
      customerEmail: 'new@test.com',
      customerName: 'New User',
      totalRon: 50,
      createdAt: new Date().toISOString(),
      status: 'Paid',
    });
    fixture.detectChanges();

    // Second load triggered
    flushStats();
    expect(component.stats?.todayOrders).toBe(5);
  });
});
