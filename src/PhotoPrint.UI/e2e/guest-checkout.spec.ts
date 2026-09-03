import { expect, test } from '@playwright/test';
import { parseAmount, SAMPLE_PHOTO } from './support/stack';

test.describe('Checkout ca vizitator', () => {
  test('un vizitator încarcă o fotografie, o adaugă în coș și ajunge la recapitulare cu totalul corect', async ({
    page,
  }) => {
    await page.goto('/');
    await page
      .getByRole('link', { name: /Tipărește acum/ })
      .first()
      .click();
    await expect(page).toHaveURL(/\/tipareste$/);

    const firstProduct = page.locator('a.product-card').first();
    await expect(
      firstProduct,
      'catalogul este gol — rulează seed-ul înainte de teste',
    ).toBeVisible();
    await firstProduct.click();
    await expect(page).toHaveURL(/\/tipareste\/[0-9a-fA-F-]{36}$/);

    await page.locator('input[type="file"]').setInputFiles(SAMPLE_PHOTO);
    await expect(page.locator('.photo-strip__item')).toHaveCount(1);
    await expect(page.locator('app-quantity-stepper')).toHaveCount(1);

    const firstSize = page.locator('label.size-option').first();
    await firstSize.click();
    await expect(firstSize).toHaveClass(/size-option--selected/);

    const summaryTotal = page.locator('.summary__total');
    await expect(summaryTotal).toContainText('lei');
    const subtotal = parseAmount(await summaryTotal.innerText());

    const addToCart = page.getByRole('button', { name: 'Adaugă în coș' });
    await expect(addToCart).toBeEnabled();
    await addToCart.click();
    await expect(page.locator('.cart-success-banner')).toBeVisible();

    await page.goto('/cos');
    await expect(page.getByRole('heading', { name: /Coșul tău/ })).toBeVisible();
    await expect(page.locator('.order-group')).toHaveCount(1);
    const cartSubtotal = parseAmount(
      await page.locator('.order-group__summary-row strong').first().innerText(),
    );
    expect(cartSubtotal).toBeCloseTo(subtotal, 2);

    await page.getByRole('link', { name: /Finalizează comanda/ }).click();
    await expect(page).toHaveURL(/\/checkout\/livrare$/);

    const courierCard = page
      .locator('.delivery-card')
      .filter({ has: page.locator('input[value="Courier"]') });
    const courier = courierCard.locator('input[value="Courier"]');
    await expect(courier, 'costurile de livrare nu s-au încărcat').toBeEnabled();
    const shipping = parseAmount(await courierCard.locator('.card-price').innerText());
    await courier.check();

    await page.locator('#street').fill('Strada Memorandumului');
    await page.locator('#number').fill('28');
    await page.locator('#city').fill('Cluj-Napoca');
    await page.locator('#county').selectOption('Cluj');
    await page.locator('#postalCode').fill('400114');
    await page.locator('#phone').fill('0722334455');
    await page.locator('#recipientName').fill('Test Vizitator');

    await page.getByRole('button', { name: /Continuă/ }).click();
    await expect(page).toHaveURL(/\/checkout\/recapitulare$/);

    await expect(page.getByRole('heading', { name: 'Recapitulare comandă' })).toBeVisible();
    const grandTotal = parseAmount(await page.locator('.total-row--grand').innerText());
    expect(grandTotal).toBeCloseTo(subtotal + shipping, 2);
    await expect(page.locator('.delivery-summary')).toContainText('Cluj-Napoca');
  });
});
