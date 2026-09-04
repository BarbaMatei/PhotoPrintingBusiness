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
    accountSvc.updateAccount.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
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
      throwError(() => new HttpErrorResponse({ status: 400 })),
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
  describe('rendering', () => {
    const el = () => fixture.nativeElement as HTMLElement;

    it('renders the three cards a password account gets', () => {
      expect(el().querySelector('app-personal-info-form')).toBeTruthy();
      expect(el().querySelector('app-password-change-form')).toBeTruthy();
      expect(el().querySelector('app-account-deletion-card')).toBeTruthy();
    });

    it('fills the personal-info fields from the account and shows the email read-only', () => {
      const firstName = el().querySelector('#firstName') as HTMLInputElement;
      const email = el().querySelector('#email') as HTMLInputElement;

      expect(firstName, 'câmpurile din componenta copil nu s-au randat').toBeTruthy();
      expect(firstName.value).toBe(component.profileForm.get('firstName')!.value);
      expect(email.disabled).toBe(true);
    });

    it('hides the password card for an account without a password', () => {
      component.account.set({ ...component.account()!, hasPassword: false });
      fixture.detectChanges();

      expect(el().querySelector('app-password-change-form')).toBeNull();
      expect(el().querySelector('app-personal-info-form')).toBeTruthy();
    });

    it('surfaces the password error the container holds', () => {
      component.passwordError.set('Parola actuală este incorectă.');
      fixture.detectChanges();

      expect(el().textContent).toContain('Parola actuală este incorectă.');
    });

    it('asks for confirmation before requesting deletion, then calls the container', () => {
      const trigger = Array.from(el().querySelectorAll('app-account-deletion-card button')).find(
        (b) => (b.textContent ?? '').includes('Solicită ștergerea'),
      ) as HTMLButtonElement;
      trigger.click();
      fixture.detectChanges();

      expect(el().textContent).toContain('Ești sigur că vrei să soliciți ștergerea contului?');
      const confirm = Array.from(el().querySelectorAll('app-account-deletion-card button')).find(
        (b) => (b.textContent ?? '').includes('Confirm ștergerea'),
      ) as HTMLButtonElement;
      confirm.click();

      expect(accountSvc.requestDeletion).toHaveBeenCalled();
    });

    it('submits the personal-info form through the container', () => {
      const form = el().querySelector('app-personal-info-form form') as HTMLFormElement;
      form.dispatchEvent(new Event('submit'));

      expect(accountSvc.updateAccount).toHaveBeenCalled();
    });
  });
});
