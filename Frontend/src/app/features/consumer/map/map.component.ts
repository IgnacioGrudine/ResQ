import {
  Component, OnInit, OnDestroy, inject, signal, viewChild, ElementRef, AfterViewInit
} from '@angular/core';
import { Router } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import {
  LucideMapPin,
  LucideStar,
  LucideX,
  LucideChevronRight
} from '@lucide/angular';
import { CatalogService } from '../../../core/services/catalog.service';
import { environment } from '../../../../environments/environment';
import { MerchantListItem } from '../../../core/models/catalog.models';

// Default map center: Córdoba, Argentina
const CORDOBA_CENTER = { lat: -31.4201, lng: -64.1888 };

// Minimal typing for the Google Maps JS API loaded at runtime via <script>.
declare const google: any;

/**
 * Loads the Google Maps JS API exactly once, reusing the same API key wiring
 * (environment.googleMapsApiKey -> __GOOGLE_MAPS_API_KEY__ placeholder) as the
 * rest of the app. Returns a shared promise so concurrent callers don't inject
 * the script twice.
 */
let mapsLoaderPromise: Promise<void> | null = null;
function loadGoogleMaps(): Promise<void> {
  if (typeof google !== 'undefined' && google.maps) return Promise.resolve();
  if (mapsLoaderPromise) return mapsLoaderPromise;

  mapsLoaderPromise = new Promise<void>((resolve, reject) => {
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.googleMapsApiKey}`;
    script.async = true;
    script.defer = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('No se pudo cargar Google Maps'));
    document.head.appendChild(script);
  });
  return mapsLoaderPromise;
}

@Component({
  selector: 'app-consumer-map',
  standalone: true,
  imports: [DecimalPipe, LucideMapPin, LucideStar, LucideX, LucideChevronRight],
  templateUrl: './map.component.html'
})
export class MapComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly catalog = inject(CatalogService);
  private readonly router  = inject(Router);

  readonly mapEl = viewChild.required<ElementRef<HTMLDivElement>>('mapEl');

  readonly loading  = signal(true);
  readonly error    = signal<string | null>(null);
  readonly selected = signal<MerchantListItem | null>(null);

  private map: any = null;
  private merchants: MerchantListItem[] = [];
  private markers: any[] = [];
  private viewReady = false;

  ngOnInit(): void {
    this.catalog.getCatalog().subscribe({
      next: list => {
        this.merchants = list;
        this.loading.set(false);
        this.tryRender();
      },
      error: () => {
        this.error.set('No se pudieron cargar los comercios.');
        this.loading.set(false);
      }
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.tryRender();
  }

  /** Renders the map once both the data has arrived and the view is ready. */
  private tryRender(): void {
    if (!this.viewReady || this.loading() || this.error()) return;

    loadGoogleMaps()
      .then(() => this.initMap())
      .catch(() => this.error.set('No se pudo cargar el mapa.'));
  }

  private initMap(): void {
    this.map = new google.maps.Map(this.mapEl().nativeElement, {
      center: CORDOBA_CENTER,
      zoom: 13,
      mapTypeControl: false,
      streetViewControl: false,
      fullscreenControl: false,
      clickableIcons: false
    });

    this.addMerchantMarkers();
    this.locateUser();
  }

  private addMerchantMarkers(): void {
    for (const m of this.merchants) {
      const marker = new google.maps.Marker({
        position: { lat: Number(m.latitude), lng: Number(m.longitude) },
        map: this.map,
        title: m.businessName
      });
      marker.addListener('click', () => this.selected.set(m));
      this.markers.push(marker);
    }
  }

  /** Centers on the user if geolocation is granted; degrades gracefully otherwise. */
  private locateUser(): void {
    if (!navigator.geolocation) return;
    navigator.geolocation.getCurrentPosition(
      pos => {
        const here = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        this.map.setCenter(here);
        new google.maps.Marker({
          position: here,
          map: this.map,
          title: 'Estás acá',
          icon: {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 8,
            fillColor: '#3b82f6',
            fillOpacity: 1,
            strokeColor: '#ffffff',
            strokeWeight: 3
          }
        });
      },
      () => { /* denied or unavailable — keep Córdoba center */ },
      { enableHighAccuracy: true, timeout: 8000 }
    );
  }

  closeCard(): void { this.selected.set(null); }

  openMerchant(id: number): void {
    this.router.navigate(['/merchant', id]);
  }

  stars(rating: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  ngOnDestroy(): void {
    for (const marker of this.markers) marker.setMap?.(null);
    this.markers = [];
  }
}
