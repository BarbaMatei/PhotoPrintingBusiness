import { expect, test } from '@playwright/test';
import { adminAccessToken, API_URL, loginAsAdmin } from './support/stack';

interface AdminOrderRow {
  id: string;
  orderNumber: string;
  status: string;
}

const SEEDED_PAID_ORDERS = ['FT-2026-0006', 'FT-2026-0007'];

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

    const seeded = SEEDED_PAID_ORDERS.map(
      (orderNumber) =>
        orders.find((o) => o.orderNumber === orderNumber) ?? {
          id: '',
          orderNumber,
          status: 'lipsă',
        },
    );
    const target = seeded.find((o) => o.status === 'Paid');
    expect(
      target,
      'nicio comandă din seed în starea Paid — observat: ' +
        seeded.map((o) => `${o.orderNumber}=${o.status}`).join(', ') +
        '. Fiecare încercare consumă o tranziție Paid → Printing și nimic nu o readuce în Paid, ' +
        'deci pe o bază deja folosită rulează seed-ul din nou pe o bază curată ' +
        '(docker compose down -v).',
    ).toBeDefined();

    let hubFrames = 0;
    page.on('websocket', (ws) => {
      if (ws.url().includes('/hubs/admin-orders')) ws.on('framereceived', () => (hubFrames += 1));
    });

    await page.goto('/admin/comenzi');
    await expect(page.locator('.ord-table__row').first()).toBeVisible();

    const row = page.locator('.ord-table__row').filter({ hasText: target!.orderNumber });
    await expect(row).toHaveCount(1);
    await expect(row.locator('.ord-badge')).toHaveText('Plătită');

    await expect
      .poll(() => hubFrames, {
        message:
          'clientul nu a primit niciun cadru pe /hubs/admin-orders — conexiunea SignalR nu s-a ' +
          'stabilit, deci actualizarea nu poate ajunge în pagina deschisă',
        timeout: 30_000,
      })
      .toBeGreaterThan(0);

    await page.evaluate(() => {
      (window as Window & { __e2eSameDocument?: boolean }).__e2eSameDocument = true;
    });

    const patch = await request.patch(`${API_URL}/admin/orders/${target!.id}/status`, {
      headers: auth,
      data: { status: 'Printing' },
    });
    expect(patch.ok(), `PATCH status a răspuns ${patch.status()}`).toBeTruthy();

    await expect(row.locator('.ord-badge')).toHaveText('În tipărire', { timeout: 20_000 });

    const sameDocument = await page.evaluate(
      () => (window as Window & { __e2eSameDocument?: boolean }).__e2eSameDocument === true,
    );
    expect(sameDocument, 'pagina s-a reîncărcat — actualizarea nu a venit prin SignalR').toBe(true);
  });
});
