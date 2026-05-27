import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  OnDestroy,
  AfterViewInit,
  ElementRef,
  ViewChild,
  SimpleChanges,
  ChangeDetectionStrategy,
} from '@angular/core';
import { LockerDto } from '../../../core/models/shipping.model';

@Component({
  selector: 'app-locker-map',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div #mapContainer class="locker-map"></div>`,
  styles: [`
    .locker-map {
      width: 100%;
      height: 420px;
      border-radius: 12px;
      border: 1px solid #e8eaed;
      background: #f8f9fa;
      z-index: 0;
    }
  `],
})
export class LockerMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;

  @Input() lockers: LockerDto[] = [];
  @Input() selectedLockerId: string | null = null;
  @Output() lockerSelected = new EventEmitter<LockerDto>();

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private map: any = null;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private markers: any[] = [];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private L: any = null;
  private initialized = false;

  async ngAfterViewInit(): Promise<void> {
    await this.initMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.initialized) return;
    if (changes['lockers'] || changes['selectedLockerId']) {
      this.map?.invalidateSize();
      this.updateMarkers();
    }
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  private async initMap(): Promise<void> {
    try {
      // Use divIcons with inline SVG — no external PNG assets required
      this.L = await import('leaflet');
      const L = this.L;

      this.map = L.map(this.mapContainer.nativeElement, { preferCanvas: true })
        .setView([45.9432, 24.9668], 7);

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 18,
      }).addTo(this.map);

      this.initialized = true;
      // Trigger a resize to ensure tiles fill the container after CSS layout
      setTimeout(() => this.map?.invalidateSize(), 150);
      if (this.lockers.length > 0) {
        this.updateMarkers();
      }
    } catch {
      // Leaflet unavailable (e.g., test environment) — fail silently
    }
  }

  private updateMarkers(): void {
    if (!this.map || !this.L) return;
    const L = this.L;

    // Clear existing markers
    this.markers.forEach(m => m.remove());
    this.markers = [];

    if (this.lockers.length === 0) return;

    const selectedSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="32" viewBox="0 0 24 32">
      <path d="M12 0C5.373 0 0 5.373 0 12c0 9 12 20 12 20s12-11 12-20C24 5.373 18.627 0 12 0z" fill="#1a73e8"/>
      <circle cx="12" cy="12" r="5" fill="#fff"/>
    </svg>`;

    const defaultSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="28" viewBox="0 0 24 32">
      <path d="M12 0C5.373 0 0 5.373 0 12c0 9 12 20 12 20s12-11 12-20C24 5.373 18.627 0 12 0z" fill="#5f6368"/>
      <circle cx="12" cy="12" r="5" fill="#fff"/>
    </svg>`;

    const greenIcon = L.divIcon({
      className: '',
      html: selectedSvg,
      iconSize: [24, 32],
      iconAnchor: [12, 32],
      popupAnchor: [0, -32],
    });

    const defaultIcon = L.divIcon({
      className: '',
      html: defaultSvg,
      iconSize: [20, 28],
      iconAnchor: [10, 28],
      popupAnchor: [0, -28],
    });

    const bounds: [number, number][] = [];

    for (const locker of this.lockers) {
      const isSelected = locker.id === this.selectedLockerId;
      const icon = isSelected ? greenIcon : defaultIcon;

      const marker = L.marker([locker.lat, locker.lng], { icon })
        .addTo(this.map)
        .bindPopup(`<strong>${locker.name}</strong><br>${locker.address}`)
        .on('click', () => this.lockerSelected.emit(locker));

      this.markers.push(marker);
      bounds.push([locker.lat, locker.lng]);
    }

    if (bounds.length > 0) {
      this.map.fitBounds(bounds, { padding: [40, 40] });
    }
  }
}
