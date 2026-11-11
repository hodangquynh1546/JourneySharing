import { AfterViewInit, Component, ElementRef, EventEmitter, OnDestroy, Output, ViewChild } from '@angular/core';
import * as L from 'leaflet';
import { Geolocation } from '@capacitor/geolocation';

export type LocationPoint = { latitude: number; longitude: number };

@Component({
  selector: 'app-location-picker',
  standalone: true,
  template: `
    <div class="map-tools" role="group" aria-label="Chọn điểm trên bản đồ">
      <button type="button" [class.selected]="target === 'origin'" (click)="target = 'origin'">Chọn điểm đi</button>
      <button type="button" [class.selected]="target === 'destination'" (click)="target = 'destination'">Chọn điểm đến</button>
      <button type="button" (click)="useCurrentLocation()">Dùng vị trí hiện tại cho điểm đi</button>
    </div>
    <div #mapHost class="picker-map" aria-label="Bản đồ chọn điểm đi và điểm đến"></div>
    @if (message) { <p class="map-message" role="status">{{ message }}</p> }
  `,
  styles: [`
    .map-tools { display: flex; flex-wrap: wrap; gap: 8px; margin: 8px 0; }
    .map-tools button { border: 1px solid #b8d2bf; background: white; color: #216a52; border-radius: 10px; padding: 9px 12px; min-height: 44px; font-weight: 700; }
    .map-tools button.selected { background: #216a52; color: white; }
    .picker-map { width: 100%; height: 320px; border-radius: 14px; border: 1px solid #d9e3d8; }
    .map-message { color: #647c71; font-size: 13px; }
    @media(max-width:560px) { .picker-map { height: 260px; } .map-tools button { flex: 1 1 45%; font-size: 12px; } }
  `]
})
export class LocationPicker implements AfterViewInit, OnDestroy {
  @ViewChild('mapHost', { static: true }) private mapHost!: ElementRef<HTMLElement>;
  @Output() readonly picked = new EventEmitter<{ target: 'origin' | 'destination'; point: LocationPoint }>();
  target: 'origin' | 'destination' = 'origin';
  message = 'Chọn điểm đi trên bản đồ, sau đó chọn điểm đến.';
  private map?: L.Map;
  private readonly markers: Partial<Record<'origin' | 'destination', L.CircleMarker>> = {};

  ngAfterViewInit(): void {
    this.map = L.map(this.mapHost.nativeElement, { zoomControl: true }).setView([16.46, 107.59], 7);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(this.map);
    this.map.on('click', event => this.choose(event.latlng));
  }

  private choose(point: L.LatLng): void {
    const selected = this.target;
    this.markers[selected]?.remove();
    this.markers[selected] = L.circleMarker(point, {
      radius: 9, color: selected === 'origin' ? '#216a52' : '#a35a24',
      fillOpacity: 1, weight: 3
    }).addTo(this.map!);
    this.picked.emit({ target: selected, point: { latitude: point.lat, longitude: point.lng } });
    this.message = selected === 'origin' ? 'Đã chọn điểm đi. Hãy chọn điểm đến.' : 'Đã chọn điểm đến.';
    if (selected === 'origin') this.target = 'destination';
  }

  async useCurrentLocation(): Promise<void> {
    try {
      const position = await Geolocation.getCurrentPosition({ enableHighAccuracy: false, timeout: 10000 });
      this.target = 'origin';
      const point = L.latLng(position.coords.latitude, position.coords.longitude);
      this.map?.setView(point, 14);
      this.choose(point);
    } catch {
      this.message = 'Không lấy được vị trí. Hãy cho phép truy cập vị trí hoặc chọn trên bản đồ.';
    }
  }

  ngOnDestroy(): void { this.map?.remove(); }
}
