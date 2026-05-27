import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Routes } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ProfilePage } from './profile-page';
import { AccountService } from '../../../../core/services/account.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { AccountDto } from '../../../../core/models/account.model';

const TEST_ROUTES: Routes = [{ path: '**', redirectTo: '' }];

const MOCK_ACCOUNT: AccountDto = {
  firstName: 'Ion',
  lastName: 'Popescu',
  email: 'ion@example.com',
  phone: '0712345678',
  hasPassword: true,
  linkedProviders: [],
  deletionRequested: false,
};

function mockAccountService() {
  return {
    getAccount: vi.fn().mockReturnValue(of(MOCK_ACCOUNT)),
    updateAccount: vi.fn().mockReturnValue(of(MOCK_ACCOUNT)),
    changePassword: vi.fn().mockReturnValue(of(undefined)),
    requestDeletion: vi.fn().mockReturnValue(of(undefined)),
  };
}

describe('ProfilePage', () => {
  let fixture: ComponentFixture<ProfilePage>;
  let component: ProfilePage;
  let accountSvc: ReturnType<typeof mockAccountService>;
  let toast: { show: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    accountSvc = mockAccountService();
    toast = { show: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [
        provideRouter(TEST_ROUTES),
        { provide: AccountService, useValue: accountSvc },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfilePage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads account on init and populates form', () => {
    expect(accountSvc.getAccount).toHaveBeenCalled();
    expect(component.account()).toEqual(MOCK_ACCOUNT);
    expect(component.profileForm.value.firstName).toBe('Ion');
    expect(component.profileForm.value.lastName).toBe('Popescu');
  });

  it('loading is false after account loads', () => {
    expect(component.loading()).toBe(false);
  });

  it('profileForm is invalid when firstName is empty', () => {
    component.profileForm.patchValue({ firstName: '' });
    expect(component.profileForm.valid).toBe(false);
  });

  it('saveProfile calls updateAccount with form values', () => {
    component.profileForm.patchValue({ firstName: 'Maria', lastName: 'Ion', phone: '' });
    component.saveProfile();
    expect(accountSvc.updateAccount).toHaveBeenCalledWith({
      firstName: 'Maria',
      lastName: 'Ion',
      phone: null,
    });
  });

  it('saveProfile shows success toast on success', () => {
    component.profileForm.patchValue({ firstName: 'Maria', lastName: 'Ion', phone: '' });
    component.saveProfile();
    expect(toast.show).toHaveBeenCalledWith('Profilul a fost actualizat.', 'success');
  });

  it('saveProfile shows error toast on failure', () => {
    accountSvc.updateAccount.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    component.profileForm.patchValue({ firstName: 'Maria', lastName: 'Ion', phone: '' });
    component.saveProfile();
    expect(toast.show).toHaveBeenCalledWith('Eroare la salvarea profilului.', 'error');
  });

  it('changePassword does not submit when form is invalid', () => {
    component.changePassword();
    expect(accountSvc.changePassword).not.toHaveBeenCalled();
  });

  it('changePassword calls service with correct payload', () => {
    component.passwordForm.patchValue({
      currentPassword: 'OldPass1!',
      newPassword: 'NewPass1!',
      confirmNewPassword: 'NewPass1!',
    });
    component.changePassword();
    expect(accountSvc.changePassword).toHaveBeenCalledWith({
      currentPassword: 'OldPass1!',
      newPassword: 'NewPass1!',
      confirmNewPassword: 'NewPass1!',
    });
  });

  it('changePassword shows error message on 400', () => {
    accountSvc.changePassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 }))
    );
    component.passwordForm.patchValue({
      currentPassword: 'Wrong1!',
      newPassword: 'NewPass1!',
      confirmNewPassword: 'NewPass1!',
    });
    component.changePassword();
    expect(component.passwordError()).toBe('Parola actuală este incorectă.');
  });

  it('requestDeletion calls service and updates account', () => {
    component.requestDeletion();
    expect(accountSvc.requestDeletion).toHaveBeenCalled();
    expect(component.account()?.deletionRequested).toBe(true);
  });
});
