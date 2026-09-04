import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FormControl } from '@angular/forms';
import { LockerSelector } from './locker-selector';
import { LockerMapComponent } from './locker-map';
import { LockerDto } from '../../../core/models/shipping.model';

const locker = (id: string, name = 'Box ' + id): LockerDto => ({
  id,
  samedayId: 'SD' + id,
  name,
  address: 'Str. ' + id,
  city: 'Cluj',
  lat: 46,
  lng: 23,
});

describe('LockerSelector', () => {
  function render(
    options: {
      lockers?: LockerDto[];
      selectedLockerId?: string | null;
      searchFailed?: boolean;
      showError?: boolean;
      search?: string;
    } = {},
  ) {
    const control = new FormControl<string | null>(options.search ?? '');
    const fixture = TestBed.createComponent(LockerSelector);
    fixture.componentRef.setInput('lockers', options.lockers ?? []);
    fixture.componentRef.setInput('selectedLockerId', options.selectedLockerId ?? null);
    fixture.componentRef.setInput('searchControl', control);
    fixture.componentRef.setInput('searchFailed', options.searchFailed ?? false);
    fixture.componentRef.setInput('showError', options.showError ?? false);
    fixture.detectChanges();
    return { fixture, control, el: fixture.nativeElement as HTMLElement };
  }

  it('renders one entry per locker and marks the selected one', () => {
    const { el } = render({ lockers: [locker('l1'), locker('l2')], selectedLockerId: 'l2' });

    const items = Array.from(el.querySelectorAll('.locker-item'));
    expect(items).toHaveLength(2);
    expect(items[0].textContent).toContain('Box l1');
    expect(items[0].textContent).toContain('Str. l1');
    expect(items[0].classList).not.toContain('selected');
    expect(items[1].classList).toContain('selected');
  });

  it('emits the clicked locker to the container', () => {
    const { fixture, el } = render({ lockers: [locker('l1'), locker('l2')] });
    const chosen: LockerDto[] = [];
    fixture.componentInstance.lockerSelected.subscribe((l) => chosen.push(l));

    (el.querySelectorAll('.locker-item')[1] as HTMLButtonElement).click();

    expect(chosen).toHaveLength(1);
    expect(chosen[0].id).toBe('l2');
  });

  it('hands the map the same lockers and selection, and forwards a pin click', () => {
    const { fixture } = render({ lockers: [locker('l1'), locker('l2')], selectedLockerId: 'l1' });
    const map = fixture.debugElement.query(By.directive(LockerMapComponent));
    expect(map).not.toBeNull();

    const mapComponent = map.componentInstance as LockerMapComponent;
    expect(mapComponent.lockers.map((l) => l.id)).toEqual(['l1', 'l2']);
    expect(mapComponent.selectedLockerId).toBe('l1');

    const chosen: LockerDto[] = [];
    fixture.componentInstance.lockerSelected.subscribe((l) => chosen.push(l));
    mapComponent.lockerSelected.emit(locker('l2'));

    expect(chosen.map((l) => l.id)).toEqual(['l2']);
  });

  it('offers a retry when the search failed, and says nothing about an empty city', () => {
    const { fixture, el } = render({ searchFailed: true, search: 'Cluj' });
    let retries = 0;
    fixture.componentInstance.retry.subscribe(() => retries++);

    expect(el.querySelector('.search-error')).not.toBeNull();
    expect(el.querySelector('.no-lockers')).toBeNull();

    (el.querySelector('.retry-link') as HTMLButtonElement).click();

    expect(retries).toBe(1);
  });

  it('reports an empty city only once one has been typed', () => {
    const { fixture, control, el } = render();
    expect(el.querySelector('.no-lockers')).toBeNull();

    control.setValue('Cluj');
    fixture.detectChanges();

    expect(el.querySelector('.no-lockers')?.textContent).toContain('Niciun easybox');
  });

  it('shows the "pick a locker" error only when the container asks for it', () => {
    expect(render().el.querySelector('.field-error')).toBeNull();
    expect(render({ showError: true }).el.querySelector('.field-error')).not.toBeNull();
  });
});
