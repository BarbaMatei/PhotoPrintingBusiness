import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccountDto,
  ChangePasswordRequest,
  SavedAddressDto,
  SavedAddressRequest,
  UpdateAccountRequest,
} from '../models/account.model';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/account`;

  getAccount(): Observable<AccountDto> {
    return this.http.get<AccountDto>(this.base);
  }

  updateAccount(req: UpdateAccountRequest): Observable<AccountDto> {
    return this.http.patch<AccountDto>(this.base, req);
  }

  changePassword(req: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/change-password`, req);
  }

  requestDeletion(): Observable<void> {
    return this.http.delete<void>(this.base);
  }

  getAddresses(): Observable<SavedAddressDto[]> {
    return this.http.get<SavedAddressDto[]>(`${this.base}/addresses`);
  }

  addAddress(req: SavedAddressRequest): Observable<SavedAddressDto> {
    return this.http.post<SavedAddressDto>(`${this.base}/addresses`, req);
  }

  updateAddress(id: string, req: SavedAddressRequest): Observable<SavedAddressDto> {
    return this.http.put<SavedAddressDto>(`${this.base}/addresses/${id}`, req);
  }

  deleteAddress(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/addresses/${id}`);
  }
}
