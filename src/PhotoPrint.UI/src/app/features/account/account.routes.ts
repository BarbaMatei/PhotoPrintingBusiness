import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/account-layout').then((m) => m.AccountLayout),
    children: [
      { path: '', redirectTo: 'profil', pathMatch: 'full' },
      {
        path: 'profil',
        loadComponent: () => import('./pages/profile/profile-page').then((m) => m.ProfilePage),
      },
      {
        path: 'adrese',
        loadComponent: () =>
          import('./pages/saved-addresses/saved-addresses-page').then((m) => m.SavedAddressesPage),
      },
    ],
  },
];
