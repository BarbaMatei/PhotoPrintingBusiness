import { TestBed, ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RouterTestingModule } from '@angular/router/testing';
import { BreadcrumbComponent } from './breadcrumb.component';

describe('BreadcrumbComponent', () => {
  let fixture: ComponentFixture<BreadcrumbComponent>;
  let component: BreadcrumbComponent;

  function createComponent(inputs: { title: string; backLink: string; backLabel?: string }) {
    fixture = TestBed.createComponent(BreadcrumbComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('title', inputs.title);
    fixture.componentRef.setInput('backLink', inputs.backLink);
    if (inputs.backLabel !== undefined) {
      fixture.componentRef.setInput('backLabel', inputs.backLabel);
    }
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BreadcrumbComponent, RouterTestingModule],
    }).compileComponents();
  });

  it('should create', () => {
    createComponent({ title: 'Test', backLink: '/admin' });
    expect(component).toBeTruthy();
  });

  it('renders a <nav> with aria-label', () => {
    createComponent({ title: 'Pagina test', backLink: '/admin' });
    const nav = fixture.nativeElement.querySelector('nav.breadcrumb');
    expect(nav).toBeTruthy();
    expect(nav.getAttribute('aria-label')).toBe('Navigare ierarhică');
  });

  it('renders the back link with the provided backLink URL', () => {
    createComponent({ title: 'Comenzi', backLink: '/admin/comenzi' });
    const anchor = fixture.debugElement.query(By.css('.breadcrumb__back'));
    expect(anchor).toBeTruthy();
    // RouterLink sets href after detectChanges
    const href = anchor.nativeElement.getAttribute('href');
    expect(href).toBe('/admin/comenzi');
  });

  it('renders the default backLabel "Înapoi" when not supplied', () => {
    createComponent({ title: 'Detalii', backLink: '/admin' });
    const anchor: HTMLElement = fixture.nativeElement.querySelector('.breadcrumb__back');
    expect(anchor.textContent).toContain('Înapoi');
  });

  it('renders a custom backLabel when supplied', () => {
    createComponent({ title: 'Detalii', backLink: '/admin/comenzi', backLabel: 'Înapoi la comenzi' });
    const anchor: HTMLElement = fixture.nativeElement.querySelector('.breadcrumb__back');
    expect(anchor.textContent).toContain('Înapoi la comenzi');
  });

  it('renders the title text', () => {
    createComponent({ title: 'Comandă FT-2026-0006', backLink: '/admin/comenzi' });
    const titleEl: HTMLElement = fixture.nativeElement.querySelector('.breadcrumb__title');
    expect(titleEl).toBeTruthy();
    expect(titleEl.textContent?.trim()).toBe('Comandă FT-2026-0006');
  });

  it('renders the separator element', () => {
    createComponent({ title: 'Test', backLink: '/admin' });
    const sep: HTMLElement = fixture.nativeElement.querySelector('.breadcrumb__sep');
    expect(sep).toBeTruthy();
    expect(sep.textContent?.trim()).toBe('/');
  });

  it('back link has btn--ghost class for consistent styling', () => {
    createComponent({ title: 'Test', backLink: '/admin' });
    const anchor: HTMLElement = fixture.nativeElement.querySelector('.breadcrumb__back');
    expect(anchor.classList.contains('btn')).toBe(true);
    expect(anchor.classList.contains('btn--ghost')).toBe(true);
  });
});
