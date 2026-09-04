import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { ProductService } from '../../core/services/product.service';
import { PricingTier } from '../../core/models/product.model';
import { HeroSection } from './components/hero-section/hero-section';
import { FormatStrip } from './components/format-strip/format-strip';
import { HowItWorks } from './components/how-it-works/how-it-works';
import { QualityHighlight } from './components/quality-highlight/quality-highlight';
import { PricingTeaser, PricingTeaserCard } from './components/pricing-teaser/pricing-teaser';
import { CtaBanner } from './components/cta-banner/cta-banner';

@Component({
  selector: 'app-home-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HeroSection, FormatStrip, HowItWorks, QualityHighlight, PricingTeaser, CtaBanner],
  template: `
    <app-hero-section />
    <app-format-strip />
    <app-how-it-works />
    <app-quality-highlight />
    <app-pricing-teaser [cards]="pricingCards()" [productName]="pricingProductName()" />
    <app-cta-banner />
  `,
})
export class HomePage implements OnInit {
  private readonly productService = inject(ProductService);

  private readonly catalogSignal = signal<{ name: string; tiers: PricingTier[] } | null>(null);

  readonly pricingProductName = computed(() => this.catalogSignal()?.name ?? '');

  readonly pricingCards = computed<PricingTeaserCard[]>(() => {
    const tiers = this.catalogSignal()?.tiers ?? [];
    const labels = ['Standard', 'Popular', 'Volum'];
    return tiers.slice(0, 3).map((tier, i) => ({
      range:
        tier.maxQuantity !== null
          ? `${tier.minQuantity}–${tier.maxQuantity} buc`
          : `${tier.minQuantity}+ buc`,
      unitPrice: tier.unitPrice,
      tierLabel: labels[i] ?? `Nivel ${i + 1}`,
    }));
  });

  ngOnInit(): void {
    this.productService.getCatalog().subscribe({
      next: (products) => {
        const first = products.find((p) => p.sizes.length > 0);
        if (!first) return;
        const firstSize = first.sizes[0];
        this.catalogSignal.set({
          name: `${first.name} – ${firstSize.label}`,
          tiers: firstSize.pricingTiers,
        });
      },
      error: () => this.catalogSignal.set(null),
    });
  }
}
