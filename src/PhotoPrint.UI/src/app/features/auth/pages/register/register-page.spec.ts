import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Routes } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { RegisterPage } from './register-page';
import { AuthService } from '../../../../core/services/auth.service';

const TEST_ROUTES: Routes = [{ path: '**', redirectTo: '' }];

function mockAuthService() {
  return {
    register: vi.fn(),
  };
}

describe('RegisterPage', () => {
  let fixture: ComponentFixture<RegisterPage>;
  let component: RegisterPage;
  let auth: ReturnType<typeof mockAuthService>;

  beforeEach(async () => {
    auth = mockAuthService();
    await TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [
        provideRouter(TEST_ROUTES),
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(RegisterPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('form starts invalid', () => {
    expect(component.form.valid).toBe(false);
  });

  it('does not call auth.register when form is invalid', () => {
    component.submit();
    expect(auth.register).not.toHaveBeenCalled();
  });

  it('form is valid with all required fields filled correctly', () => {
    component.form.setValue({
      firstName: 'Ion',
      lastName: 'Pop',
      email: 'ion@pop.ro',
      password: 'StrongP@ss1',
      confirmPassword: 'StrongP@ss1',
      phone: '',
      gdprConsent: true,
    });
    expect(component.form.valid).toBe(true);
  });

  it('form is invalid when passwords do not match', () => {
    component.form.setValue({
      firstName: 'Ion',
      lastName: 'Pop',
      email: 'ion@pop.ro',
      password: 'StrongP@ss1',
      confirmPassword: 'Different1@',
      phone: '',
      gdprConsent: true,
    });
    expect(component.form.valid).toBe(false);
    expect(component.form.errors?.['passwordMismatch']).toBe(true);
  });

  it('form is invalid when gdprConsent is false', () => {
    component.form.setValue({
      firstName: 'Ion',
      lastName: 'Pop',
      email: 'ion@pop.ro',
      password: 'StrongP@ss1',
      confirmPassword: 'StrongP@ss1',
      phone: '',
      gdprConsent: false,
    });
    expect(component.form.valid).toBe(false);
  });

  it('strengthErrors returns null for a strong password', () => {
    component.form.get('password')!.setValue('StrongP@ss1');
    expect(component.strengthErrors).toBeNull();
  });

  it('strengthErrors returns errors for a weak password', () => {
    component.form.get('password')!.setValue('weak');
    expect(component.strengthErrors).toBeTruthy();
  });

  it('calls auth.register with form values on valid submit', () => {
    auth.register.mockReturnValue(of({ message: 'ok' }));
    component.form.setValue({
      firstName: 'Ion',
      lastName: 'Pop',
      email: 'ion@pop.ro',
      password: 'StrongP@ss1',
      confirmPassword: 'StrongP@ss1',
      phone: '',
      gdprConsent: true,
    });
    component.submit();
    expect(auth.register).toHaveBeenCalledWith(
      expect.objectContaining({ email: 'ion@pop.ro', firstName: 'Ion' }),
    );
  });

  it('sets email conflict error on 409 response', () => {
    auth.register.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component.form.setValue({
      firstName: 'Ion',
      lastName: 'Pop',
      email: 'ion@pop.ro',
      password: 'StrongP@ss1',
      confirmPassword: 'StrongP@ss1',
      phone: '',
      gdprConsent: true,
    });
    component.submit();
    expect(component.form.get('email')!.errors?.['conflict']).toBe(true);
  });

  it('maps API validation errors to form fields on 422 response', () => {
    auth.register.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 422,
            error: { errors: { Email: ['Email already taken'] } },
          }),
      ),
    );
    component.form.setValue({
      firstName: 'Ion',
      lastName: 'Pop',
      email: 'ion@pop.ro',
      password: 'StrongP@ss1',
      confirmPassword: 'StrongP@ss1',
      phone: '',
      gdprConsent: true,
    });
    component.submit();
    expect(component.form.get('email')!.errors?.['apiError']).toBe('Email already taken');
  });

  it('resets loading to false on error', () => {
    auth.register.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component.form.setValue({
      firstName: 'Ion',
      lastName: 'Pop',
      email: 'ion@pop.ro',
      password: 'StrongP@ss1',
      confirmPassword: 'StrongP@ss1',
      phone: '',
      gdprConsent: true,
    });
    component.submit();
    expect(component.loading).toBe(false);
  });
});
