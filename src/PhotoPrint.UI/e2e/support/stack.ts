import { expect, Page } from '@playwright/test';
import { join } from 'node:path';

export const API_URL = process.env['E2E_API_URL'] ?? 'http://localhost:5052/api';

export const ADMIN_EMAIL = process.env['E2E_ADMIN_EMAIL'] ?? 'mateibarba@yahoo.com';
export const ADMIN_PASSWORD = process.env['E2E_ADMIN_PASSWORD'] ?? 'Admin1234!';

export const SAMPLE_PHOTO = join(__dirname, '..', 'fixtures', 'sample-photo.jpg');

export async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/auth/login');
  await page.locator('#email').fill(ADMIN_EMAIL);
  await page.locator('#password').fill(ADMIN_PASSWORD);
  await page.getByRole('button', { name: 'Autentifică-te' }).click();
  await page.waitForFunction(() => sessionStorage.getItem('access_token') !== null, null, {
    timeout: 15_000,
  });
}

export async function adminAccessToken(page: Page): Promise<string> {
  const token = await page.evaluate(() => sessionStorage.getItem('access_token'));
  expect(token, 'tokenul de admin lipsește din sessionStorage').toBeTruthy();
  return token as string;
}

/**
 * Reads the first money amount out of rendered text. The app registers no LOCALE_ID, so
 * `number:'1.2-2'` emits en-US (`1,234.56`); the shape assertion fails loudly if that ever
 * changes, because a ro-RO `1.234,56` would otherwise parse to a wrong number silently.
 */
export function parseAmount(text: string): number {
  const match = text.replace(/\s/g, '').match(/-?\d[\d,]*\.\d{2}/);
  expect(match, `nu am găsit o sumă în format en-US în «${text}»`).not.toBeNull();
  return parseFloat((match as RegExpMatchArray)[0].replace(/,/g, ''));
}
