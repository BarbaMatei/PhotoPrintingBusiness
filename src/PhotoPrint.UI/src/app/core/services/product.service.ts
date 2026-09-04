import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { BaseApiService } from './api/base-api.service';
import { Product } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly api = inject(BaseApiService);
  private readonly base = '/products';

  private readonly catalog$$ = new BehaviorSubject<Product[] | null>(null);

  /** Returns the cached catalog, or fetches from API on first call. */
  getCatalog(): Observable<Product[]> {
    const cached = this.catalog$$.value;
    if (cached !== null) {
      return new Observable(observer => {
        observer.next(cached);
        observer.complete();
      });
    }
    return this.api.get<Product[]>(this.base).pipe(
      tap(products => this.catalog$$.next(products)),
    );
  }

  /** Fetches a single active product by ID. */
  getProduct(id: string): Observable<Product> {
    return this.api.get<Product>(`${this.base}/${id}`);
  }

  /** Clears the in-memory cache (e.g. after admin update). */
  clearCache(): void {
    this.catalog$$.next(null);
  }
}
