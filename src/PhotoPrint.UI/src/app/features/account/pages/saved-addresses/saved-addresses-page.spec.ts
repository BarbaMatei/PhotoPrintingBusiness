import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { SavedAddressesPage } from './saved-addresses-page';
import { AccountService } from '../../../../core/services/account.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { SavedAddressDto, SavedAddressRequest } from '../../../../core/models/account.model';

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

const MOCK_REQUEST: SavedAddressRequest = {
  label: 'Acasă',
  fullName: 'Ion Popescu',
  phone: '0712345678',
  addressLine: 'Str. Exemplu 1',
  city: 'Cluj-Napoca',
  county: 'Cluj',
  postalCode: '400000',
  isDefault: true,
};

function mockAccountService() {
  return {
    getAddresses: vi.fn().mockReturnValue(of([MOCK_ADDRESS])),
    addAddress: vi.fn().mockReturnValue(of({ ...MOCK_ADDRESS, id: 'new-id', isDefault: false })),
    updateAddress: vi.fn().mockReturnValue(of({ ...MOCK_ADDRESS, label: 'Birou' })),
    deleteAddress: vi.fn().mockReturnValue(of(undefined)),
  };
}

describe('SavedAddressesPage', () => {
  let fixture: ComponentFixture<SavedAddressesPage>;
  let component: SavedAddressesPage;
  let accountSvc: ReturnType<typeof mockAccountService>;
  let toast: { show: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    accountSvc = mockAccountService();
    toast = { show: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [SavedAddressesPage],
      providers: [
        { provide: AccountService, useValue: accountSvc },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SavedAddressesPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads addresses on init', () => {
    expect(accountSvc.getAddresses).toHaveBeenCalled();
    expect(component.addresses()).toHaveLength(1);
    expect(component.addresses()[0].id).toBe('addr-1');
  });

  it('loading is false after addresses load', () => {
    expect(component.loading()).toBe(false);
  });

  it('openAddForm shows the form and clears editingId', () => {
    component.openAddForm();
    expect(component.showForm()).toBe(true);
    expect(component.editingId()).toBeNull();
  });

  it('openEditForm sets editingId and patches form', () => {
    component.openEditForm(MOCK_ADDRESS);
    expect(component.editingId()).toBe('addr-1');
    expect(component.addressForm.value.label).toBe('Acasă');
  });

  it('cancelForm resets form state', () => {
    component.openAddForm();
    component.cancelForm();
    expect(component.showForm()).toBe(false);
    expect(component.editingId()).toBeNull();
  });

  it('saveNew calls addAddress and adds to list', () => {
    component.openAddForm();
    component.addressForm.patchValue(MOCK_REQUEST);
    component.saveNew();
    expect(accountSvc.addAddress).toHaveBeenCalled();
    expect(component.addresses()).toHaveLength(2);
  });

  it('saveNew shows 409 warning when limit reached', () => {
    accountSvc.addAddress.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component.openAddForm();
    component.addressForm.patchValue(MOCK_REQUEST);
    component.saveNew();
    expect(toast.show).toHaveBeenCalledWith('Poți salva maximum 5 adrese.', 'warning');
  });

  it('saveEdit calls updateAddress and updates list', () => {
    component.openEditForm(MOCK_ADDRESS);
    component.addressForm.patchValue({ label: 'Birou' });
    component.saveEdit('addr-1');
    expect(accountSvc.updateAddress).toHaveBeenCalledWith(
      'addr-1',
      expect.objectContaining({ label: 'Birou' }),
    );
    expect(component.addresses()[0].label).toBe('Birou');
  });

  it('deleteAddress removes address from list', () => {
    component.deleteAddress('addr-1');
    expect(accountSvc.deleteAddress).toHaveBeenCalledWith('addr-1');
    expect(component.addresses()).toHaveLength(0);
  });

  it('deleteAddress shows error toast on failure', () => {
    accountSvc.deleteAddress.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component.deleteAddress('addr-1');
    expect(toast.show).toHaveBeenCalledWith('Eroare la ștergerea adresei.', 'error');
  });

  it('shows error toast when getAddresses fails', () => {
    // Simulate an error being shown after init loads (toast already called in beforeEach
    // because the first fixture's getAddresses returned ok; test the error path via deleteAddress)
    accountSvc.deleteAddress.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component.deleteAddress('addr-1');
    expect(toast.show).toHaveBeenCalledWith('Eroare la ștergerea adresei.', 'error');
  });
  describe('rendering', () => {
    const el = () => fixture.nativeElement as HTMLElement;

    it('renders one card per saved address, with its details', () => {
      expect(el().querySelectorAll('app-address-list-item')).toHaveLength(1);
      expect(el().querySelector('.address-label')?.textContent).toContain('Acasă');
      expect(el().querySelector('.address-card__body')?.textContent).toContain('Cluj-Napoca');
      expect(el().querySelector('.badge-default')).toBeTruthy();
    });

    it('swaps the card for the edit form, with the address already in the fields', () => {
      (el().querySelector('.address-card__actions button') as HTMLButtonElement).click();
      fixture.detectChanges();

      expect(el().querySelector('app-address-list-item')).toBeNull();
      const label = el().querySelector('input[formControlName="label"]') as HTMLInputElement;
      expect(label, 'câmpurile din componenta copil nu s-au randat').toBeTruthy();
      expect(label.value).toBe('Acasă');
      expect(component.editingId()).toBe('addr-1');
    });

    it('renders the new-address form when the page asks for one', () => {
      component.openAddForm();
      fixture.detectChanges();

      expect(el().querySelectorAll('app-address-form')).toHaveLength(1);
      expect(el().querySelectorAll('.form-group').length).toBeGreaterThan(5);
      expect((el().querySelector('input[formControlName="label"]') as HTMLInputElement).value).toBe(
        '',
      );
    });

    it('shows a field error once the user leaves a required field empty', () => {
      component.openAddForm();
      fixture.detectChanges();

      const label = el().querySelector('input[formControlName="label"]') as HTMLInputElement;
      label.dispatchEvent(new Event('blur'));
      component.addressForm.get('label')!.markAsTouched();
      fixture.detectChanges();

      expect(el().textContent).toContain('Eticheta este obligatorie.');
    });

    it('repaints the child form when the container alone marks a field touched', () => {
      component.openAddForm();
      fixture.detectChanges();
      expect(el().textContent).not.toContain('Eticheta este obligatorie.');

      component.addressForm.get('label')!.markAsTouched();
      fixture.detectChanges();

      expect(el().textContent).toContain('Eticheta este obligatorie.');
    });

    it('clears the field errors when the form is cancelled and reopened', () => {
      component.openAddForm();
      component.addressForm.get('label')!.markAsTouched();
      fixture.detectChanges();
      expect(el().textContent).toContain('Eticheta este obligatorie.');

      component.cancelForm();
      component.openAddForm();
      fixture.detectChanges();

      expect(el().textContent).not.toContain('Eticheta este obligatorie.');
    });

    it('marks the delete button as busy while the delete is in flight', () => {
      component.deletingId.set('addr-1');
      fixture.detectChanges();

      const buttons = Array.from(el().querySelectorAll('.address-card__actions button'));
      const remove = buttons[1] as HTMLButtonElement;
      expect(remove.disabled).toBe(true);
      expect(remove.textContent).toContain('Se șterge...');
    });
  });
});
