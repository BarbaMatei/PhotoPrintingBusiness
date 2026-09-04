import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './api/base-api.service';
import {
  AccountDto,
  ChangePasswordRequest,
  SavedAddressDto,
  SavedAddressRequest,
  UpdateAccountRequest,
} from '../models/account.model';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly api = inject(BaseApiService);
  private readonly base = '/account';

  getAccount(): Observable<AccountDto> {
    return this.api.get<AccountDto>(this.base);
  }

  updateAccount(req: UpdateAccountRequest): Observable<AccountDto> {
    return this.api.patch<AccountDto>(this.base, req);
  }

  changePassword(req: ChangePasswordRequest): Observable<void> {
    return this.api.post<void>(`${this.base}/change-password`, req);
  }

  requestDeletion(): Observable<void> {
    return this.api.delete<void>(this.base);
  }

  getAddresses(): Observable<SavedAddressDto[]> {
    return this.api.get<SavedAddressDto[]>(`${this.base}/addresses`);
  }

  addAddress(req: SavedAddressRequest): Observable<SavedAddressDto> {
    return this.api.post<SavedAddressDto>(`${this.base}/addresses`, req);
  }

  updateAddress(id: string, req: SavedAddressRequest): Observable<SavedAddressDto> {
    return this.api.put<SavedAddressDto>(`${this.base}/addresses/${id}`, req);
  }

  deleteAddress(id: string): Observable<void> {
    return this.api.delete<void>(`${this.base}/addresses/${id}`);
  }
}
