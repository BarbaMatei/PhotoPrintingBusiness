import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ProductService } from '../../../../core/services/product.service';
import { Product } from '../../../../core/models/product.model';
import { ProductCardComponent } from '../../../../shared/components/product-card/product-card';

@Component({
  selector: 'app-catalog-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ProductCardComponent],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.scss',
})
export class CatalogPage implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly cdr = inject(ChangeDetectorRef);

  products: Product[] = [];
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.loadCatalog();
  }

  loadCatalog(): void {
    this.loading = true;
    this.error = null;
    this.cdr.markForCheck();

    this.productService.getCatalog().subscribe({
      next: products => {
        this.products = products;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'Nu am putut încărca produsele. Încearcă din nou.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }
}
