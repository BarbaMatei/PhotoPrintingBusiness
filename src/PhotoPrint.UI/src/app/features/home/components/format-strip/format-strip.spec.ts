import { TestBed } from '@angular/core/testing';
import { FormatStrip } from './format-strip';

describe('FormatStrip', () => {
  function render() {
    const fixture = TestBed.createComponent(FormatStrip);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('renders the label list twice, which is what makes the marquee loop seamlessly', () => {
    const fixture = render();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelectorAll('.format-strip__item')).toHaveLength(
      fixture.componentInstance.formatLabels.length * 2,
    );
  });

  it('is hidden from assistive technology, being decorative', () => {
    const el = render().nativeElement as HTMLElement;

    expect(el.querySelector('.format-strip')?.getAttribute('aria-hidden')).toBe('true');
  });
});
