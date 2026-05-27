import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ToastComponent } from './toast';
import { ToastService } from '../../services/toast.service';

describe('ToastComponent', () => {
  let fixture: ComponentFixture<ToastComponent>;
  let component: ToastComponent;
  let toastService: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ToastComponent);
    component = fixture.componentInstance;
    toastService = TestBed.inject(ToastService);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders no toast items when service has no toasts', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.toast').length).toBe(0);
  });

  it('renders a toast item when the service emits a toast', async () => {
    toastService.show('Saved!', 'success');
    await fixture.whenStable();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.toast').length).toBe(1);
    expect(el.querySelector('.toast__message')?.textContent).toBe('Saved!');
  });

  it('applies the correct type class to the toast', async () => {
    toastService.show('Error!', 'error');
    await fixture.whenStable();
    fixture.detectChanges();
    const toast = fixture.nativeElement.querySelector('.toast') as HTMLElement;
    expect(toast.classList.contains('toast--error')).toBe(true);
  });

  it('renders multiple toasts', async () => {
    toastService.show('A', 'info');
    toastService.show('B', 'warning');
    await fixture.whenStable();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.toast').length).toBe(2);
  });

  it('dismiss button removes the toast', async () => {
    toastService.show('Removable', 'info');
    await fixture.whenStable();
    fixture.detectChanges();
    const closeBtn = fixture.nativeElement.querySelector('.toast__close') as HTMLButtonElement;
    closeBtn.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.toast').length).toBe(0);
  });
});
