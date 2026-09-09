import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './api/base-api.service';
import { Product, ProductSize } from '../models/product.model';

export interface CreateProductRequest {
  name: string;
  productType: string;
  imageUrl: string | null;
  sortOrder: number;
  sizes: CreateProductSizeRequest[];
}

export interface CreateProductSizeRequest {
  label: string;
  widthMm: number;
  heightMm: number;
}

export interface UpdateProductRequest {
  name: string;
  productType: string;
  imageUrl: string | null;
  sortOrder: number;
}

export interface ReplacePricingTiersRequest {
  tiers: CreatePricingTierRequest[];
}

export interface CreatePricingTierRequest {
  minQuantity: number;
  maxQuantity: number | null;
  unitPrice: number;
}

@Injectable({ providedIn: 'root' })
export class ProductAdminService {
  private readonly api = inject(BaseApiService);
  private readonly base = '/admin/products';

  getAdminProducts(): Observable<Product[]> {
    return this.api.get<Product[]>(this.base);
  }

  createProduct(request: CreateProductRequest): Observable<Product> {
    return this.api.post<Product>(this.base, request);
  }

  updateProduct(id: string, request: UpdateProductRequest): Observable<Product> {
    return this.api.put<Product>(`${this.base}/${id}`, request);
  }

  setProductStatus(id: string, isActive: boolean): Observable<{ id: string; isActive: boolean }> {
    return this.api.patch<{ id: string; isActive: boolean }>(`${this.base}/${id}/status`, { isActive });
  }

  deleteProduct(id: string): Observable<void> {
    return this.api.delete<void>(`${this.base}/${id}`);
  }

  addSize(productId: string, request: CreateProductSizeRequest): Observable<ProductSize> {
    return this.api.post<ProductSize>(`${this.base}/${productId}/sizes`, request);
  }

  setSizeStatus(productId: string, sizeId: string, isActive: boolean): Observable<{ id: string; isActive: boolean }> {
    return this.api.patch<{ id: string; isActive: boolean }>(
      `${this.base}/${productId}/sizes/${sizeId}/status`, { isActive },
    );
  }

  replacePricingTiers(productId: string, sizeId: string, request: ReplacePricingTiersRequest): Observable<ProductSize> {
    return this.api.put<ProductSize>(
      `${this.base}/${productId}/sizes/${sizeId}/pricing`, request,
    );
  }

  replaceFinishes(productId: string, names: string[]): Observable<void> {
    return this.api.put<void>(`${this.base}/${productId}/finishes`, { names });
  }
}
