import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ResetPasswordPage } from './reset-password-page';
import { AuthService } from '../../../../core/services/auth.service';

function mockAuthService() {
  return {
    resetPassword: vi.fn(),
  };
}

function makeActivatedRoute(userId: string | null, token: string | null) {
  return {
    snapshot: {
      queryParamMap: {
        get: (key: string) => {
          if (key === 'userId') return userId;
          if (key === 'token') return token;
          return null;
        },
      },
    },
  };
}

describe('ResetPasswordPage — with valid params', () => {
  let fixture: ComponentFixture<ResetPasswordPage>;
  let component: ResetPasswordPage;
  let auth: ReturnType<typeof mockAuthService>;

  beforeEach(async () => {
    auth = mockAuthService();
    await TestBed.configureTestingModule({
      imports: [ResetPasswordPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: makeActivatedRoute('user-123', 'reset-token') },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ResetPasswordPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('paramsValid is true when userId and token are present', () => {
    expect(component.paramsValid).toBe(true);
  });

  it('form starts invalid', () => {
    expect(component.form.valid).toBe(false);
  });

  it('form is valid with matching strong passwords', () => {
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'StrongP@ss1' });
    expect(component.form.valid).toBe(true);
  });

  it('form is invalid when passwords do not match', () => {
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'Different1@' });
    expect(component.form.valid).toBe(false);
    expect(component.form.errors?.['passwordMismatch']).toBe(true);
  });

  it('strengthErrors returns null for a strong password', () => {
    component.form.get('password')!.setValue('StrongP@ss1');
    expect(component.strengthErrors).toBeNull();
  });

  it('calls auth.resetPassword on valid submit', () => {
    auth.resetPassword.mockReturnValue(of(undefined));
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'StrongP@ss1' });
    component.submit();
    expect(auth.resetPassword).toHaveBeenCalledWith({
      userId: 'user-123',
      token: 'reset-token',
      newPassword: 'StrongP@ss1',
      confirmPassword: 'StrongP@ss1',
    });
  });

  it('sets succeeded to true on success', () => {
    auth.resetPassword.mockReturnValue(of(undefined));
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'StrongP@ss1' });
    component.submit();
    expect(component.succeeded).toBe(true);
  });

  it('sets apiError on 400 response with detail', () => {
    auth.resetPassword.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ status: 400, error: { detail: 'Token expirat.' } }),
      ),
    );
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'StrongP@ss1' });
    component.submit();
    expect(component.apiError).toBe('Token expirat.');
  });

  it('sets default apiError on 400 response without detail', () => {
    auth.resetPassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: {} })),
    );
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'StrongP@ss1' });
    component.submit();
    expect(component.apiError).toBe('Link invalid sau expirat.');
  });

  it('resets loading to false on success', () => {
    auth.resetPassword.mockReturnValue(of(undefined));
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'StrongP@ss1' });
    component.submit();
    expect(component.loading).toBe(false);
  });

  it('resets loading to false on error', () => {
    auth.resetPassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: {} })),
    );
    component.form.setValue({ password: 'StrongP@ss1', confirmPassword: 'StrongP@ss1' });
    component.submit();
    expect(component.loading).toBe(false);
  });
});

describe('ResetPasswordPage — with missing params', () => {
  let component: ResetPasswordPage;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService() },
        { provide: ActivatedRoute, useValue: makeActivatedRoute(null, null) },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(ResetPasswordPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('paramsValid is false when userId and token are missing', () => {
    expect(component.paramsValid).toBe(false);
  });
});
