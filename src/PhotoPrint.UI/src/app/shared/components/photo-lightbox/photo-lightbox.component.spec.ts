import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { PhotoLightboxComponent } from './photo-lightbox.component';

describe('PhotoLightboxComponent', () => {
  let fixture: ComponentFixture<PhotoLightboxComponent>;
  let cmp: PhotoLightboxComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PhotoLightboxComponent] }).compileComponents();
    fixture = TestBed.createComponent(PhotoLightboxComponent);
    cmp = fixture.componentInstance;
    document.body.appendChild(fixture.nativeElement); // so focus()/activeElement work in jsdom
  });

  afterEach(() => fixture.nativeElement.remove());

  function setSrc(src: string | null): void {
    fixture.componentRef.setInput('src', src);
    fixture.detectChanges();
  }

  const el = () => fixture.nativeElement as HTMLElement;

  it('renders nothing when src is null', () => {
    setSrc(null);
    expect(el().querySelector('.lightbox__backdrop')).toBeNull();
  });

  it('renders a labelled modal dialog when src is set (F17/D33)', () => {
    setSrc('https://cdn/x');
    const dialog = el().querySelector('.lightbox__backdrop')!;
    expect(dialog.getAttribute('role')).toBe('dialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(dialog.getAttribute('aria-label')).toBeTruthy();
  });

  it('emits close on Escape', () => {
    const spy = vi.fn();
    cmp.close.subscribe(spy);
    setSrc('https://cdn/x');
    cmp.onEscape();
    expect(spy).toHaveBeenCalledOnce();
  });

  it('emits imgError and shows the fallback when the image fails to load (F7/D5b)', () => {
    const spy = vi.fn();
    cmp.imgError.subscribe(spy);
    setSrc('https://cdn/x');

    el().querySelector<HTMLImageElement>('.lightbox__img')!.dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(spy).toHaveBeenCalledOnce();
    expect(cmp.failed()).toBe(true);
    expect(el().querySelector('.lightbox__error')).not.toBeNull();
    expect(el().querySelector('.lightbox__img')).toBeNull();
  });

  it('clears the failed state when a fresh src arrives (refreshed URL)', () => {
    setSrc('https://cdn/stale');
    el().querySelector<HTMLImageElement>('.lightbox__img')!.dispatchEvent(new Event('error'));
    fixture.detectChanges();
    expect(cmp.failed()).toBe(true);

    setSrc('https://cdn/fresh');
    expect(cmp.failed()).toBe(false);
    expect(el().querySelector('.lightbox__img')).not.toBeNull();
  });

  it('moves focus to the close button on open and restores it to the trigger on close (F17/D33)', () => {
    const trigger = document.createElement('button');
    document.body.appendChild(trigger);
    trigger.focus();
    expect(document.activeElement).toBe(trigger);

    setSrc('https://cdn/x');
    expect(document.activeElement).toBe(el().querySelector('.lightbox__close'));

    setSrc(null);
    expect(document.activeElement).toBe(trigger);

    trigger.remove();
  });
});
