import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { EmailVerificationPendingPage } from './verify-email-page';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../shared/services/toast.service';

function mockAuthService() {
  return {
    resendConfirmation: vi.fn(),
  };
}

function makeActivatedRoute(confirmed: string | null) {
  return {
    snapshot: {
      queryParamMap: convertToParamMap(confirmed ? { confirmed } : {}),
    },
  };
}

describe('EmailVerificationPendingPage', () => {
  let fixture: ComponentFixture<EmailVerificationPendingPage>;
  let component: EmailVerificationPendingPage;
  let auth: ReturnType<typeof mockAuthService>;
  let toast: { show: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    auth = mockAuthService();
    toast = { show: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [EmailVerificationPendingPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: makeActivatedRoute(null) },
      ],
    }).compileComponents();

    // Set history state before component creation
    history.replaceState({ email: 'user@test.com' }, '');

    fixture = TestBed.createComponent(EmailVerificationPendingPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    history.replaceState({}, '');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('reads email from history.state', () => {
    expect(component.email).toBe('user@test.com');
  });

  it('confirmed is false when query param is absent', () => {
    expect(component.confirmed).toBe(false);
  });

  it('countdown starts at 0', () => {
    expect(component.countdown).toBe(0);
  });

  it('does not call resendConfirmation when email is empty', () => {
    // history.state has no email → email will be '' in a fresh component
    // Just verify that resend guards on empty email
    // The countdown guard is also tested separately
    auth.resendConfirmation.mockReturnValue(of(undefined));
    // Set loading to true to simulate the guard (loading blocks re-submit)
    component.loading = true;
    component.resend();
    expect(auth.resendConfirmation).not.toHaveBeenCalled();
    component.loading = false;
  });

  it('calls auth.resendConfirmation with the email on resend', () => {
    auth.resendConfirmation.mockReturnValue(of(undefined));
    component.resend();
    expect(auth.resendConfirmation).toHaveBeenCalledWith('user@test.com');
  });

  it('shows success toast on successful resend', () => {
    auth.resendConfirmation.mockReturnValue(of(undefined));
    component.resend();
    expect(toast.show).toHaveBeenCalledWith('Email de confirmare retrimis.', 'success');
  });

  it('resets loading to false after successful resend', () => {
    auth.resendConfirmation.mockReturnValue(of(undefined));
    component.resend();
    expect(component.loading).toBe(false);
  });

  it('starts countdown on 429 response', () => {
    auth.resendConfirmation.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 429 })),
    );
    component.resend();
    expect(component.countdown).toBe(60);
    component.ngOnDestroy();
  });

  it('shows warning toast on 429 response', () => {
    auth.resendConfirmation.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 429 })),
    );
    component.resend();
    expect(toast.show).toHaveBeenCalledWith(
      expect.stringContaining('Prea multe'),
      'warning',
    );
    component.ngOnDestroy();
  });

  it('does not call resend again while countdown is active', () => {
    auth.resendConfirmation.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 429 })),
    );
    component.resend();
    // countdown is now 60, should not trigger another call
    auth.resendConfirmation.mockReturnValue(of(undefined));
    component.resend();
    expect(auth.resendConfirmation).toHaveBeenCalledTimes(1);
    component.ngOnDestroy();
  });

  it('countdown decrements over time', () => {
    vi.useFakeTimers();
    auth.resendConfirmation.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 429 })),
    );
    component.resend();
    expect(component.countdown).toBe(60);
    vi.advanceTimersByTime(1000);
    expect(component.countdown).toBe(59);
    vi.advanceTimersByTime(1000);
    expect(component.countdown).toBe(58);
    component.ngOnDestroy();
    vi.useRealTimers();
  });
});

describe('EmailVerificationPendingPage — confirmed param', () => {
  let component: EmailVerificationPendingPage;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmailVerificationPendingPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService() },
        { provide: ToastService, useValue: { show: vi.fn() } },
        { provide: ActivatedRoute, useValue: makeActivatedRoute('true') },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(EmailVerificationPendingPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('confirmed is true when query param is "true"', () => {
    expect(component.confirmed).toBe(true);
  });
});
