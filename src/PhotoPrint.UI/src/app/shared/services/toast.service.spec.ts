import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('starts with no toasts', () => {
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    expect(values[0]).toHaveLength(0);
  });

  it('show() adds a toast with the given message and type', () => {
    service.show('Hello', 'success');
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    expect(values[0]).toHaveLength(1);
    expect((values[0][0] as any).message).toBe('Hello');
    expect((values[0][0] as any).type).toBe('success');
  });

  it('show() defaults to type "info" when no type provided', () => {
    service.show('msg');
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    expect((values[0][0] as any).type).toBe('info');
  });

  it('show() assigns a unique id to each toast', () => {
    service.show('A', 'info');
    service.show('B', 'error');
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    const ids = (values[0] as any[]).map((t: any) => t.id);
    expect(new Set(ids).size).toBe(2);
  });

  it('dismiss() removes the toast with the matching id', () => {
    service.show('test', 'warning');
    let id: string;
    service.toasts$.subscribe(toasts => {
      if ((toasts as any[]).length === 1) {
        id = (toasts[0] as any).id;
      }
    });
    service.dismiss(id!);
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    expect(values[0]).toHaveLength(0);
  });

  it('dismiss() is a no-op for an unknown id', () => {
    service.show('keep', 'info');
    service.dismiss('no-such-id');
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    expect(values[0]).toHaveLength(1);
  });

  it('auto-dismisses toast after 5 seconds', () => {
    service.show('fade out', 'info');
    vi.advanceTimersByTime(5000);
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    expect(values[0]).toHaveLength(0);
  });

  it('toast survives before the 5-second mark', () => {
    service.show('still here', 'info');
    vi.advanceTimersByTime(4999);
    const values: unknown[][] = [];
    service.toasts$.subscribe(v => values.push(v));
    expect(values[0]).toHaveLength(1);
  });
});
