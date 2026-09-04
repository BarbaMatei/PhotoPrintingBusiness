import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface PricingTeaserCard {
  range: string;
  unitPrice: number;
  tierLabel: string;
}

@Component({
  selector: 'app-pricing-teaser',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './pricing-teaser.html',
  styleUrl: './pricing-teaser.scss',
})
export class PricingTeaser {
  readonly cards = input.required<PricingTeaserCard[]>();
  readonly productName = input<string>('');
}
