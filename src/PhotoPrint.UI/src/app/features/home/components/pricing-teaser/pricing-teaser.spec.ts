import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PricingTeaser, PricingTeaserCard } from './pricing-teaser';

const card = (tierLabel: string, unitPrice: number): PricingTeaserCard => ({
  range: '1–9 buc',
  unitPrice,
  tierLabel,
});

describe('PricingTeaser', () => {
  function render(cards: PricingTeaserCard[], productName = '') {
    const fixture = TestBed.createComponent(PricingTeaser);
    fixture.componentRef.setInput('cards', cards);
    fixture.componentRef.setInput('productName', productName);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('falls back to three built-in cards when the catalog gave none', () => {
    const el = render([]).nativeElement as HTMLElement;

    const prices = Array.from(el.querySelectorAll('.pricing-tease__price')).map((p) =>
      (p.textContent ?? '').trim(),
    );
    expect(prices).toHaveLength(3);
    expect(prices[0]).toContain('1.20');
    expect(el.querySelector('.pricing-tease__card--popular')).toBeTruthy();
  });

  it('renders a single tier without inventing the other two', () => {
    const el = render([card('Standard', 1.2)]).nativeElement as HTMLElement;

    expect(el.querySelectorAll('.pricing-tease__card')).toHaveLength(1);
    expect(el.querySelector('.pricing-tease__price')?.textContent).toContain('1.20');
  });

  it('marks the middle card as the popular one for three tiers', () => {
    const el = render([card('Standard', 1.2), card('Popular', 0.99), card('Volum', 0.89)])
      .nativeElement as HTMLElement;

    const cards = Array.from(el.querySelectorAll('.pricing-tease__card'));
    expect(cards).toHaveLength(3);
    expect(cards[1].classList).toContain('pricing-tease__card--popular');
    expect(cards[0].classList).not.toContain('pricing-tease__card--popular');
  });

  it('renders a fourth tier if it is given one, still marking only the second', () => {
    const el = render([
      card('Standard', 1.2),
      card('Popular', 0.99),
      card('Volum', 0.89),
      card('Nivel 4', 0.79),
    ]).nativeElement as HTMLElement;

    const cards = Array.from(el.querySelectorAll('.pricing-tease__card'));
    expect(cards).toHaveLength(4);
    expect(cards.filter((c) => c.classList.contains('pricing-tease__card--popular'))).toHaveLength(
      1,
    );
  });

  it('names the product only when one was resolved', () => {
    expect(
      (render([card('Standard', 1.2)]).nativeElement as HTMLElement).querySelector(
        '.pricing-tease__source',
      ),
    ).toBeNull();

    const withName = render([card('Standard', 1.2)], 'Fotografii clasice – 10×15')
      .nativeElement as HTMLElement;
    expect(withName.querySelector('.pricing-tease__source')?.textContent).toContain(
      'Fotografii clasice – 10×15',
    );
  });
});
