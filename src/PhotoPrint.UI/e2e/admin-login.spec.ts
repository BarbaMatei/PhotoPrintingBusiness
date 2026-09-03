import { expect, test } from '@playwright/test';
import { ADMIN_EMAIL, ADMIN_PASSWORD } from './support/stack';

test.describe('Autentificare administrator', () => {
  test('un administrator neautentificat este trimis la login și revine în panoul de administrare', async ({
    page,
  }) => {
    await page.goto('/admin');
    await expect(page).toHaveURL(/\/auth\/login$/);

    await page.locator('#email').fill(ADMIN_EMAIL);
    await page.locator('#password').fill(ADMIN_PASSWORD);
    await page.getByRole('button', { name: 'Autentifică-te' }).click();

    await expect(page).toHaveURL(/\/admin$/);
    await expect(page.getByRole('heading', { name: 'Administrare' })).toBeVisible();
    await expect(page.getByRole('link', { name: /Comenzi/ }).first()).toBeVisible();

    const token = await page.evaluate(() => sessionStorage.getItem('access_token'));
    expect(token, 'login-ul nu a stocat un token de acces').toBeTruthy();
  });
});
