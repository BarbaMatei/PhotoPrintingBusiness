import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Routes } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { LoginPage } from './login-page';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../shared/services/toast.service';

const TEST_ROUTES: Routes = [{ path: '**', redirectTo: '' }];

function mockAuthService() {
  return {
    login: vi.fn(),
    getReturnUrl: vi.fn().mockReturnValue('/tipareste'),
    setReturnUrl: vi.fn(),
    resendConfirmation: vi.fn(),
  };
}

describe('LoginPage', () => {
  let fixture: ComponentFixture<LoginPage>;
  let component: LoginPage;
  let auth: ReturnType<typeof mockAuthService>;

  beforeEach(async () => {
    auth = mockAuthService();
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter(TEST_ROUTES),
        { provide: AuthService, useValue: auth },
        { provide: ToastService, useValue: { show: vi.fn() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(LoginPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('form starts invalid', () => {
    expect(component.form.valid).toBe(false);
  });

  it('form is valid with email and password', () => {
    component.form.setValue({ email: 'user@test.com', password: 'secret', rememberMe: false });
    expect(component.form.valid).toBe(true);
  });

  it('does not call auth.login when form is invalid', () => {
    component.submit();
    expect(auth.login).not.toHaveBeenCalled();
  });

  it('calls auth.login with the form values on valid submit', () => {
    auth.login.mockReturnValue(of({
      accessToken: 'tok',
      userId: '1',
      email: 'user@test.com',
      displayName: 'User',
      isAdmin: false,
    }));
    component.form.setValue({ email: 'user@test.com', password: 'Pass1@', rememberMe: false });
    component.submit();
    expect(auth.login).toHaveBeenCalledWith('user@test.com', 'Pass1@');
  });

  it('sets formError on 401 response', () => {
    auth.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    component.form.setValue({ email: 'user@test.com', password: 'wrong', rememberMe: false });
    component.submit();
    expect(component.formError).toBe('Email sau parolă incorectă.');
  });

  it('sets formError and resendVisible on 403 response', () => {
    auth.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 403 })));
    component.form.setValue({ email: 'user@test.com', password: 'pass', rememberMe: false });
    component.submit();
    expect(component.resendVisible).toBe(true);
    expect(component.formError).toBeTruthy();
  });

  it('sets locked message containing remaining minutes on 423 response', () => {
    auth.login.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 423, error: { remainingMinutes: 10 } })),
    );
    component.form.setValue({ email: 'user@test.com', password: 'pass', rememberMe: false });
    component.submit();
    expect(component.formError).toContain('10');
  });

  it('resets loading to false on error', () => {
    auth.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    component.form.setValue({ email: 'user@test.com', password: 'wrong', rememberMe: false });
    component.submit();
    expect(component.loading).toBe(false);
  });

  it('clears formError before each submit', () => {
    auth.login.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 401 })));
    auth.login.mockReturnValueOnce(of({ accessToken: 'tok', userId: '1', email: 'e', displayName: 'd', isAdmin: false }));

    component.form.setValue({ email: 'user@test.com', password: 'pass', rememberMe: false });
    component.submit();
    expect(component.formError).toBeTruthy();

    component.submit();
    expect(component.formError).toBeNull();
  });

  it('resendConfirmation calls auth.resendConfirmation with stored email', () => {
    auth.resendConfirmation.mockReturnValue(of(undefined));
    component['resendEmail'] = 'user@test.com';
    component.resendConfirmation();
    expect(auth.resendConfirmation).toHaveBeenCalledWith('user@test.com');
  });
});
