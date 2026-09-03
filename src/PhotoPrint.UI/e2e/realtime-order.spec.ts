import { expect, test } from '@playwright/test';
import { adminAccessToken, API_URL, loginAsAdmin } from './support/stack';

interface AdminOrderRow {
  id: string;
  orderNumber: string;
  status: string;
}

test.describe('Actualizare în timp real a comenzilor (SignalR)', () => {
  test('schimbarea statusului unei comenzi ajunge în lista deschisă de admin fără reîncărcare', async ({
    page,
    request,
  }) => {
    await loginAsAdmin(page);
    const token = await adminAccessToken(page);
    const auth = { Authorization: `Bearer ${token}` };

    const listResponse = await request.get(`${API_URL}/admin/orders?page=1&pageSize=50`, {
      headers: auth,
    });
    expect(listResponse.ok(), `GET /admin/orders a răspuns ${listResponse.status()}`).toBeTruthy();
    const orders: AdminOrderRow[] = (await listResponse.json()).items;

    const target = orders.find((o) => o.status === 'Paid');
    test.skip(
      !target,
      'nicio comandă în starea Paid — rulează seed-ul pe o bază curată (docker compose down -v)',
    );

    const hubConnected = page.waitForRequest(
      (req) => req.url().includes('/hubs/admin-orders') && req.url().includes('id='),
      { timeout: 30_000 },
    );
    await page.goto('/admin/comenzi');
    await expect(page.locator('.ord-table__row').first()).toBeVisible();

    const row = page.locator('.ord-table__row').filter({ hasText: target!.orderNumber });
    await expect(row).toHaveCount(1);
    await expect(row.locator('.ord-badge')).toHaveText('Plătită');

    await hubConnected;

    const patch = await request.patch(`${API_URL}/admin/orders/${target!.id}/status`, {
      headers: auth,
      data: { status: 'Printing' },
    });
    expect(patch.ok(), `PATCH status a răspuns ${patch.status()}`).toBeTruthy();

    await expect(row.locator('.ord-badge')).toHaveText('În tipărire', { timeout: 20_000 });
    expect(page.url()).toContain('/admin/comenzi');
  });
});
