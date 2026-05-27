import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Product } from '../../../../core/models/product.model';
import { ProductAdminService, CreateProductRequest } from '../../../../core/services/product-admin.service';
import { ProductService } from '../../../../core/services/product.service';

@Component({
  selector: 'app-admin-products-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DecimalPipe],
  templateUrl: './admin-products-page.html',
  styleUrl: './admin-products-page.scss',
})
export class AdminProductsPage implements OnInit {
  private readonly adminService = inject(ProductAdminService);
  private readonly productService = inject(ProductService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  products: Product[] = [];
  loading = true;
  formVisible = false;
  editingProductId: string | null = null;
  editingProduct: Product | null = null;
  formError: string | null = null;
  pricingEditorSizeId: string | null = null;
  pricingEditorProductId: string | null = null;
  pricingError: string | null = null;

  // Add-size (in edit modal)
  addSizeVisible = false;
  addSizeError: string | null = null;

  // Finishes editor
  finishesEditorProductId: string | null = null;
  finishesError: string | null = null;

  productForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    productType: ['PhotoPrint', Validators.required],
    imageUrl: [''],
    sortOrder: [0, [Validators.required, Validators.min(0)]],
    sizes: this.fb.array([this.createSizeGroup()]),
  });

  pricingForm = this.fb.group({
    tiers: this.fb.array([this.createTierGroup()]),
  });

  addSizeForm = this.fb.group({
    label:    ['', [Validators.required, Validators.maxLength(50)]],
    widthMm:  [100, [Validators.required, Validators.min(1)]],
    heightMm: [150, [Validators.required, Validators.min(1)]],
  });

  finishesForm = this.fb.group({
    names: this.fb.array([this.fb.control('', Validators.required)]),
  });

  get sizesArray(): FormArray {
    return this.productForm.get('sizes') as FormArray;
  }

  get tiersArray(): FormArray {
    return this.pricingForm.get('tiers') as FormArray;
  }

  get finishNamesArray(): FormArray {
    return this.finishesForm.get('names') as FormArray;
  }

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.cdr.markForCheck();
    this.adminService.getAdminProducts().subscribe({
      next: products => {
        this.products = products;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  showCreateForm(): void {
    this.editingProductId = null;
    this.productForm.reset({ productType: 'PhotoPrint', sortOrder: 0 });
    this.sizesArray.clear();
    this.sizesArray.push(this.createSizeGroup());
    this.formVisible = true;
    this.formError = null;
    this.cdr.markForCheck();
  }

  showEditForm(product: Product): void {
    this.editingProductId = product.id;
    this.editingProduct = product;
    this.productForm.patchValue({
      name: product.name,
      productType: product.productType,
      imageUrl: product.imageUrl ?? '',
      sortOrder: product.sortOrder,
    });
    this.sizesArray.clear();
    this.addSizeVisible = false;
    this.addSizeError = null;
    this.addSizeForm.reset({ label: '', widthMm: 100, heightMm: 150 });
    this.formVisible = true;
    this.formError = null;
    this.cdr.markForCheck();
  }

  cancelForm(): void {
    this.formVisible = false;
    this.editingProductId = null;
    this.editingProduct = null;
    this.addSizeVisible = false;
    this.cdr.markForCheck();
  }

  addSize(): void {
    this.sizesArray.push(this.createSizeGroup());
    this.cdr.markForCheck();
  }

  removeSize(index: number): void {
    if (this.sizesArray.length > 1) {
      this.sizesArray.removeAt(index);
      this.cdr.markForCheck();
    }
  }

  submitProductForm(): void {
    if (this.productForm.invalid) return;

    const value = this.productForm.getRawValue();
    this.formError = null;

    if (this.editingProductId) {
      this.adminService.updateProduct(this.editingProductId, {
        name: value.name!,
        productType: value.productType!,
        imageUrl: value.imageUrl || null,
        sortOrder: value.sortOrder ?? 0,
      }).subscribe({
        next: () => {
          this.formVisible = false;
          this.editingProduct = null;
          this.productService.clearCache();
          this.loadProducts();
        },
        error: (err: HttpErrorResponse) => {
          this.formError = err.error?.detail ?? 'Eroare la salvare.';
          this.cdr.markForCheck();
        },
      });
    } else {
      const request: CreateProductRequest = {
        name: value.name!,
        productType: value.productType!,
        imageUrl: value.imageUrl || null,
        sortOrder: value.sortOrder ?? 0,
        sizes: value.sizes!.map(s => ({
          label: s.label!,
          widthMm: s.widthMm!,
          heightMm: s.heightMm!,
        })),
      };
      this.adminService.createProduct(request).subscribe({
        next: () => {
          this.formVisible = false;
          this.productService.clearCache();
          this.loadProducts();
        },
        error: (err: HttpErrorResponse) => {
          this.formError = err.error?.detail ?? 'Eroare la salvare.';
          this.cdr.markForCheck();
        },
      });
    }
  }

  deleteProduct(id: string): void {
    if (!confirm('Ești sigur că vrei să ștergi acest produs?')) return;
    this.adminService.deleteProduct(id).subscribe({
      next: () => {
        this.productService.clearCache();
        this.loadProducts();
      },
    });
  }

  toggleProductStatus(product: Product): void {
    this.adminService.setProductStatus(product.id, !product.isActive).subscribe({
      next: () => {
        this.productService.clearCache();
        this.loadProducts();
      },
    });
  }

  toggleSizeStatus(product: Product, size: { id: string; isActive?: boolean }): void {
    this.adminService.setSizeStatus(product.id, size.id, !size.isActive).subscribe({
      next: () => {
        this.productService.clearCache();
        this.loadProducts();
      },
    });
  }

  showAddSizeForm(): void {
    this.addSizeVisible = true;
    this.addSizeError = null;
    this.addSizeForm.reset({ label: '', widthMm: 100, heightMm: 150 });
    this.cdr.markForCheck();
  }

  cancelAddSize(): void {
    this.addSizeVisible = false;
    this.cdr.markForCheck();
  }

  submitAddSize(): void {
    if (!this.editingProductId || this.addSizeForm.invalid) return;
    const v = this.addSizeForm.getRawValue();
    this.addSizeError = null;
    this.adminService.addSize(this.editingProductId, {
      label: v.label!,
      widthMm: v.widthMm!,
      heightMm: v.heightMm!,
    }).subscribe({
      next: () => {
        this.addSizeVisible = false;
        this.productService.clearCache();
        // Refresh and re-open the edit modal with updated data
        this.adminService.getAdminProducts().subscribe(products => {
          this.products = products;
          const updated = products.find(p => p.id === this.editingProductId);
          if (updated) this.editingProduct = updated;
          this.cdr.markForCheck();
        });
      },
      error: (err: HttpErrorResponse) => {
        this.addSizeError = err.error?.detail ?? 'Eroare la adăugarea dimensiunii.';
        this.cdr.markForCheck();
      },
    });
  }

  openFinishesEditor(product: Product): void {
    this.finishesEditorProductId = product.id;
    this.finishesError = null;
    this.finishNamesArray.clear();
    const names = product.finishes.length > 0 ? product.finishes : [''];
    names.forEach(n => this.finishNamesArray.push(this.fb.control(n, Validators.required)));
    this.cdr.markForCheck();
  }

  closeFinishesEditor(): void {
    this.finishesEditorProductId = null;
    this.cdr.markForCheck();
  }

  addFinish(): void {
    this.finishNamesArray.push(this.fb.control('', Validators.required));
    this.cdr.markForCheck();
  }

  removeFinish(index: number): void {
    if (this.finishNamesArray.length > 1) {
      this.finishNamesArray.removeAt(index);
      this.cdr.markForCheck();
    }
  }

  saveFinishes(): void {
    if (!this.finishesEditorProductId) return;
    const names = this.finishNamesArray.getRawValue() as string[];
    this.finishesError = null;
    this.adminService.replaceFinishes(this.finishesEditorProductId, names).subscribe({
      next: () => {
        this.closeFinishesEditor();
        this.productService.clearCache();
        this.loadProducts();
      },
      error: (err: HttpErrorResponse) => {
        this.finishesError = err.error?.detail ?? 'Eroare la salvarea finisajelor.';
        this.cdr.markForCheck();
      },
    });
  }

  openPricingEditor(productId: string, sizeId: string, existingTiers: { minQuantity: number; maxQuantity: number | null; unitPrice: number }[]): void {
    this.pricingEditorProductId = productId;
    this.pricingEditorSizeId = sizeId;
    this.pricingError = null;
    this.tiersArray.clear();
    if (existingTiers.length > 0) {
      existingTiers.forEach(t => this.tiersArray.push(this.createTierGroup(t)));
    } else {
      this.tiersArray.push(this.createTierGroup());
    }
    this.cdr.markForCheck();
  }

  closePricingEditor(): void {
    this.pricingEditorSizeId = null;
    this.pricingEditorProductId = null;
    this.cdr.markForCheck();
  }

  addTier(): void {
    this.tiersArray.push(this.createTierGroup());
    this.cdr.markForCheck();
  }

  removeTier(index: number): void {
    if (this.tiersArray.length > 1) {
      this.tiersArray.removeAt(index);
      this.cdr.markForCheck();
    }
  }

  savePricingTiers(): void {
    if (!this.pricingEditorProductId || !this.pricingEditorSizeId) return;
    const tiers = this.tiersArray.getRawValue();
    this.adminService.replacePricingTiers(this.pricingEditorProductId, this.pricingEditorSizeId, {
      tiers: tiers.map(t => ({
        minQuantity: t.minQuantity,
        maxQuantity: t.maxQuantity ? Number(t.maxQuantity) : null,
        unitPrice: t.unitPrice,
      })),
    }).subscribe({
      next: () => {
        this.closePricingEditor();
        this.productService.clearCache();
        this.loadProducts();
      },
      error: (err: HttpErrorResponse) => {
        this.pricingError = err.error?.detail ?? 'Eroare la validarea nivelurilor de prețuri.';
        this.cdr.markForCheck();
      },
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  private createSizeGroup() {
    return this.fb.group({
      label: ['', [Validators.required, Validators.maxLength(50)]],
      widthMm: [100, [Validators.required, Validators.min(1)]],
      heightMm: [150, [Validators.required, Validators.min(1)]],
    });
  }

  private createTierGroup(values?: { minQuantity: number; maxQuantity: number | null; unitPrice: number }) {
    return this.fb.group({
      minQuantity: [values?.minQuantity ?? 1, [Validators.required, Validators.min(1)]],
      maxQuantity: [values?.maxQuantity?.toString() ?? ''],
      unitPrice: [values?.unitPrice ?? 1.00, [Validators.required, Validators.min(0.01)]],
    });
  }
}


