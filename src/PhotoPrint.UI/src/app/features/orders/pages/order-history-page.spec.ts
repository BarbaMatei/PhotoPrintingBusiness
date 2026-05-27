import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { OrderHistoryPage } from './order-history-page';
import { OrderService } from '../../../core/services/order.service';
import { OrderSummaryDto } from '../../../core/models/order.model';

const MOCK_ORDER: OrderSummaryDto = {
  id: 'order-1',
  orderNumber: 'FT-001',
  status: 'Paid',
  totalRon: 120,
  createdAt: '2026-05-01T12:00:00Z',
  deliveryType: 'Easybox',
  itemCount: 3,
};

function makeOrderService(overrides: Partial<OrderService> = {}): Partial<OrderService> {
  return {
    getOrders: vi.fn().mockReturnValue(of({ items: [], total: 0 })),
    ...overrides,
  };
}

describe('OrderHistoryPage', () => {
  let fixture: ComponentFixture<OrderHistoryPage>;
  let component: OrderHistoryPage;

  async function setup(serviceOverrides: Partial<OrderService> = {}) {
    await TestBed.configureTestingModule({
      imports: [OrderHistoryPage],
      providers: [
        provideRouter([]),
        { provide: OrderService, useValue: makeOrderService(serviceOverrides) },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderHistoryPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('shows empty state when no orders', async () => {
    await setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Nu ai nicio comandă');
  });

  it('renders order rows when orders are returned', async () => {
    await setup({
      getOrders: vi.fn().mockReturnValue(of({ items: [MOCK_ORDER], total: 1 })),
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('FT-001');
    expect(el.textContent).toContain('Plătită');
  });

  it('shows error state when API fails', async () => {
    await setup({
      getOrders: vi.fn().mockReturnValue(throwError(() => new Error('500'))),
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('A apărut o eroare');
  });

  it('shows pagination when total exceeds page size', async () => {
    const orders = Array.from({ length: 10 }, (_, i) => ({
      ...MOCK_ORDER,
      id: `order-${i}`,
      orderNumber: `FT-00${i}`,
    }));
    await setup({
      getOrders: vi.fn().mockReturnValue(of({ items: orders, total: 25 })),
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('1 / 3');
  });

  it('does not show pagination when all orders fit on one page', async () => {
    await setup({
      getOrders: vi.fn().mockReturnValue(of({ items: [MOCK_ORDER], total: 1 })),
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.pagination')).toBeNull();
  });

  it('calls getOrders with updated page on setPage', async () => {
    const getSpy = vi.fn().mockReturnValue(of({ items: [], total: 25 }));
    await setup({ getOrders: getSpy });

    component.setPage(2);
    fixture.detectChanges();

    expect(getSpy).toHaveBeenCalledWith(2, 10);
  });
});
