import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Footer } from './footer';

describe('Footer', () => {
  let fixture: ComponentFixture<Footer>;
  let component: Footer;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Footer],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Footer);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders the current year in the copyright notice', () => {
    const el = fixture.nativeElement as HTMLElement;
    const text = el.querySelector('.footer__copy')?.textContent ?? '';
    expect(text).toContain(String(new Date().getFullYear()));
  });

  it('renders a link to the privacy policy', () => {
    const el = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('.footer__link')) as HTMLAnchorElement[];
    const hrefs = links.map(a => a.getAttribute('href') ?? '');
    expect(hrefs.some(h => h.includes('politica-de-confidentialitate'))).toBe(true);
  });

  it('renders a link to the terms and conditions', () => {
    const el = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('.footer__link')) as HTMLAnchorElement[];
    const hrefs = links.map(a => a.getAttribute('href') ?? '');
    expect(hrefs.some(h => h.includes('termeni-si-conditii'))).toBe(true);
  });

  it('renders a link to the cookie policy', () => {
    const el = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('.footer__link')) as HTMLAnchorElement[];
    const hrefs = links.map(a => a.getAttribute('href') ?? '');
    expect(hrefs.some(h => h.includes('politica-cookie'))).toBe(true);
  });
});
