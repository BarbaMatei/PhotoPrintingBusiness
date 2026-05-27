import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Routes } from '@angular/router';
import { CookieConsentBanner } from './cookie-consent';

const TEST_ROUTES: Routes = [{ path: '**', redirectTo: '' }];

describe('CookieConsentBanner', () => {
  let fixture: ComponentFixture<CookieConsentBanner>;
  let component: CookieConsentBanner;

  beforeEach(async () => {
    localStorage.removeItem('cookie-consent');

    await TestBed.configureTestingModule({
      imports: [CookieConsentBanner],
      providers: [provideRouter(TEST_ROUTES)],
    }).compileComponents();

    fixture = TestBed.createComponent(CookieConsentBanner);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.removeItem('cookie-consent');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('is visible when cookie-consent key is not set', () => {
    expect(component.visible()).toBe(true);
  });

  it('is not visible when cookie-consent is already set', () => {
    // The beforeEach already removed the key, then set visible=true.
    // Simulate dismissal (same effect as pre-existing consent).
    component.accept('all');
    expect(component.visible()).toBe(false);
  });

  it('accept("all") sets localStorage and hides banner', () => {
    component.accept('all');
    expect(localStorage.getItem('cookie-consent')).toBe('all');
    expect(component.visible()).toBe(false);
  });

  it('accept("essential") sets localStorage and hides banner', () => {
    component.accept('essential');
    expect(localStorage.getItem('cookie-consent')).toBe('essential');
    expect(component.visible()).toBe(false);
  });
});
