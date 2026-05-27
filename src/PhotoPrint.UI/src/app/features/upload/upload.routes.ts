import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/catalog/catalog-page').then(m => m.CatalogPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/format-selector/format-selector-page').then(m => m.FormatSelectorPage),
  },
];
