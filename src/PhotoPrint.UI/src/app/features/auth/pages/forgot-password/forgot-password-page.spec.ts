import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ForgotPasswordPage } from './forgot-password-page';
import { AuthService } from '../../../../core/services/auth.service';

function mockAuthService() {
  return {
    forgotPassword: vi.fn(),
  };
}

describe('ForgotPasswordPage', () => {
  let fixture: ComponentFixture<ForgotPasswordPage>;
  let component: ForgotPasswordPage;
  let auth: ReturnType<typeof mockAuthService>;

  beforeEach(async () => {
    auth = mockAuthService();
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ForgotPasswordPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('form starts invalid', () => {
    expect(component.form.valid).toBe(false);
  });

  it('does not call auth.forgotPassword when form is invalid', () => {
    component.submit();
    expect(auth.forgotPassword).not.toHaveBeenCalled();
  });

  it('form is valid with a valid email', () => {
    component.form.setValue({ email: 'user@test.com' });
    expect(component.form.valid).toBe(true);
  });

  it('submitted starts as false', () => {
    expect(component.submitted).toBe(false);
  });

  it('sets submitted to true on success response', () => {
    auth.forgotPassword.mockReturnValue(of(undefined));
    component.form.setValue({ email: 'user@test.com' });
    component.submit();
    expect(component.submitted).toBe(true);
  });

  it('sets submitted to true on error response (anti-enumeration)', () => {
    auth.forgotPassword.mockReturnValue(throwError(() => new Error('not found')));
    component.form.setValue({ email: 'user@test.com' });
    component.submit();
    expect(component.submitted).toBe(true);
  });

  it('resets loading to false after success', () => {
    auth.forgotPassword.mockReturnValue(of(undefined));
    component.form.setValue({ email: 'user@test.com' });
    component.submit();
    expect(component.loading).toBe(false);
  });

  it('resets loading to false after error', () => {
    auth.forgotPassword.mockReturnValue(throwError(() => new Error('fail')));
    component.form.setValue({ email: 'user@test.com' });
    component.submit();
    expect(component.loading).toBe(false);
  });

  it('calls auth.forgotPassword with the email value', () => {
    auth.forgotPassword.mockReturnValue(of(undefined));
    component.form.setValue({ email: 'user@test.com' });
    component.submit();
    expect(auth.forgotPassword).toHaveBeenCalledWith('user@test.com');
  });
});
