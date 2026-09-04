import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { HomePage } from './home-page';
import { environment } from '../../../environments/environment';

const CATALOG = [
  {
    id: 'p1',
    name: 'Fotografii clasice',
    productType: 'Print',
    imageUrl: null,
    sortOrder: 1,
    finishes: [],
    sizes: [
      {
        id: 's1',
        label: '10×15',
        widthMm: 100,
        heightMm: 150,
        isActive: true,
        pricingTiers: [
          { id: 't1', minQuantity: 1, maxQuantity: 9, unitPrice: 1.2 },
          { id: 't2', minQuantity: 10, maxQuantity: 49, unitPrice: 0.99 },
          { id: 't3', minQuantity: 50, maxQuantity: null, unitPrice: 0.89 },
        ],
      },
    ],
  },
];

describe('HomePage', () => {
  let http: HttpTestingController;

  function createFixture() {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('renders every section of the page', () => {
    const fixture = createFixture();
    http.expectOne(`${environment.apiUrl}/products`).flush(CATALOG);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-hero-section .hero')).toBeTruthy();
    expect(el.querySelector('app-hero-section app-photo-mosaic .photo-mosaic')).toBeTruthy();
    expect(el.querySelector('app-format-strip .format-strip')).toBeTruthy();
    expect(el.querySelector('app-how-it-works .steps')).toBeTruthy();
    expect(el.querySelector('app-quality-highlight .quality')).toBeTruthy();
    expect(el.querySelector('app-pricing-teaser .pricing-tease')).toBeTruthy();
    expect(el.querySelector('app-cta-banner .cta-block')).toBeTruthy();
  });

  it('keeps every call-to-action pointing where it did', () => {
    const fixture = createFixture();
    http.expectOne(`${environment.apiUrl}/products`).flush(CATALOG);
    fixture.detectChanges();

    const hrefs = fixture.debugElement
      .queryAll(By.css('a[href]'))
      .map((a) => (a.nativeElement as HTMLAnchorElement).getAttribute('href'));

    expect(hrefs.filter((h) => h === '/tipareste')).toHaveLength(3);
    expect(hrefs.filter((h) => h === '/preturi')).toHaveLength(2);
  });

  it('feeds the pricing teaser with the first three tiers of the first product', () => {
    const fixture = createFixture();
    http.expectOne(`${environment.apiUrl}/products`).flush(CATALOG);
    fixture.detectChanges();

    expect(fixture.componentInstance.pricingProductName()).toBe('Fotografii clasice – 10×15');
    expect(fixture.componentInstance.pricingCards()).toEqual([
      { range: '1–9 buc', unitPrice: 1.2, tierLabel: 'Standard' },
      { range: '10–49 buc', unitPrice: 0.99, tierLabel: 'Popular' },
      { range: '50+ buc', unitPrice: 0.89, tierLabel: 'Volum' },
    ]);

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.pricing-tease__card')).toHaveLength(3);
    expect(el.textContent).toContain('Fotografii clasice – 10×15');
  });

  it('still renders the page when the catalog request fails', () => {
    const fixture = createFixture();
    http
      .expectOne(`${environment.apiUrl}/products`)
      .flush('down', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-hero-section .hero')).toBeTruthy();
    expect(fixture.componentInstance.pricingCards()).toEqual([]);
  });

  it('ignores a product that has no sizes', () => {
    const fixture = createFixture();
    http
      .expectOne(`${environment.apiUrl}/products`)
      .flush([{ ...CATALOG[0], sizes: [] }, CATALOG[0]]);
    fixture.detectChanges();

    expect(fixture.componentInstance.pricingCards()).toHaveLength(3);
  });
});
