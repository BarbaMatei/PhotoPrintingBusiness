import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AccountService } from './account.service';
import { environment } from '../../../environments/environment';
import {
  AccountDto,
  ChangePasswordRequest,
  SavedAddressDto,
  SavedAddressRequest,
  UpdateAccountRequest,
} from '../models/account.model';

const BASE = `${environment.apiUrl}/account`;

const MOCK_ACCOUNT: AccountDto = {
  firstName: 'Ion',
  lastName: 'Popescu',
  email: 'ion@example.com',
  phone: '0712345678',
  hasPassword: true,
  linkedProviders: [],
  deletionRequested: false,
};

const MOCK_ADDRESS: SavedAddressDto = {
  id: 'addr-1',
  label: 'Acasă',
  fullName: 'Ion Popescu',
  phone: '0712345678',
  addressLine: 'Str. Exemplu 1',
  city: 'Cluj-Napoca',
  county: 'Cluj',
  postalCode: '400000',
  isDefault: true,
};

describe('AccountService', () => {
  let service: AccountService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AccountService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAccount GETs /account', () => {
    let result: AccountDto | undefined;
    service.getAccount().subscribe(r => (result = r));

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_ACCOUNT);

    expect(result).toEqual(MOCK_ACCOUNT);
  });

  it('updateAccount PATCHes /account', () => {
    const body: UpdateAccountRequest = { firstName: 'Maria', lastName: 'Ion', phone: null };
    let result: AccountDto | undefined;
    service.updateAccount(body).subscribe(r => (result = r));

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual(body);
    req.flush({ ...MOCK_ACCOUNT, firstName: 'Maria' });

    expect(result?.firstName).toBe('Maria');
  });

  it('changePassword POSTs /account/change-password', () => {
    const body: ChangePasswordRequest = {
      currentPassword: 'Old1!',
      newPassword: 'New1!pass',
      confirmNewPassword: 'New1!pass',
    };
    let completed = false;
    service.changePassword(body).subscribe(() => (completed = true));

    const req = http.expectOne(`${BASE}/change-password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush(null);

    expect(completed).toBe(true);
  });

  it('requestDeletion DELETEs /account', () => {
    let completed = false;
    service.requestDeletion().subscribe(() => (completed = true));

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(completed).toBe(true);
  });

  it('getAddresses GETs /account/addresses', () => {
    let result: SavedAddressDto[] | undefined;
    service.getAddresses().subscribe(r => (result = r));

    const req = http.expectOne(`${BASE}/addresses`);
    expect(req.request.method).toBe('GET');
    req.flush([MOCK_ADDRESS]);

    expect(result).toEqual([MOCK_ADDRESS]);
  });

  it('addAddress POSTs /account/addresses', () => {
    const body: SavedAddressRequest = {
      label: 'Birou',
      fullName: 'Ion Popescu',
      phone: '0712345678',
      addressLine: 'Str. Muncii 5',
      city: 'București',
      county: 'Ilfov',
      postalCode: '010101',
      isDefault: false,
    };
    let result: SavedAddressDto | undefined;
    service.addAddress(body).subscribe(r => (result = r));

    const req = http.expectOne(`${BASE}/addresses`);
    expect(req.request.method).toBe('POST');
    req.flush({ ...body, id: 'new-id', isDefault: false });

    expect(result?.id).toBe('new-id');
  });

  it('updateAddress PUTs /account/addresses/:id', () => {
    const body: SavedAddressRequest = { ...MOCK_ADDRESS };
    let result: SavedAddressDto | undefined;
    service.updateAddress('addr-1', body).subscribe(r => (result = r));

    const req = http.expectOne(`${BASE}/addresses/addr-1`);
    expect(req.request.method).toBe('PUT');
    req.flush({ ...MOCK_ADDRESS, label: 'Updated' });

    expect(result?.label).toBe('Updated');
  });

  it('deleteAddress DELETEs /account/addresses/:id', () => {
    let completed = false;
    service.deleteAddress('addr-1').subscribe(() => (completed = true));

    const req = http.expectOne(`${BASE}/addresses/addr-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(completed).toBe(true);
  });
});
