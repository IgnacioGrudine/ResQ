import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { CatalogService, PackFilters } from '../../../core/services/catalog.service';
import { Category, PackListItem } from '../../../core/models/catalog.models';
import {
  LucideSearch,
  LucideLeaf,
  LucideMapPin,
  LucideClock,
  LucideX,
  LucideRefreshCw
} from '@lucide/angular';

interface FilterCategory { id: number | null; name: string; }

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [FormsModule, DecimalPipe, LucideSearch, LucideLeaf, LucideMapPin, LucideClock, LucideX, LucideRefreshCw],
  templateUrl: './feed.component.html'
})
export class FeedComponent implements OnInit {
  private readonly catalog = inject(CatalogService);
  private readonly router  = inject(Router);

  readonly packs      = signal<PackListItem[]>([]);
  readonly categories = signal<FilterCategory[]>([{ id: null, name: 'Todos' }]);
  readonly loading    = signal(false);
  readonly error      = signal<string | null>(null);

  searchInput         = '';
  selectedCategory: number | null = null;
  selectedMaxPrice    = '';
  selectedMaxDistance = '';
  userLat: number | null = null;
  userLon: number | null = null;
  locationDenied = false;

  readonly priceOptions = [
    { value: '',     label: 'Cualquier precio' },
    { value: '500',  label: 'Hasta $500' },
    { value: '1000', label: 'Hasta $1.000' },
    { value: '2000', label: 'Hasta $2.000' },
  ];

  readonly distanceOptions = [
    { value: '',   label: 'Cualquier distancia' },
    { value: '1',  label: 'Hasta 1 km' },
    { value: '3',  label: 'Hasta 3 km' },
    { value: '5',  label: 'Hasta 5 km' },
    { value: '10', label: 'Hasta 10 km' },
  ];

  ngOnInit(): void {
    this.loadCategories();
    this.loadPacks();
    this.requestLocation();
  }

  loadCategories(): void {
    this.catalog.getCategories().subscribe({
      next: cats => this.categories.set([
        { id: null, name: 'Todos' },
        ...cats.map((c: Category) => ({ id: c.id, name: c.name }))
      ]),
      error: () => { /* keep the "Todos" fallback already in the signal */ }
    });
  }

  requestLocation(): void {
    if (!navigator.geolocation) { this.locationDenied = true; return; }
    navigator.geolocation.getCurrentPosition(
      pos => {
        this.userLat = pos.coords.latitude;
        this.userLon = pos.coords.longitude;
        this.loadPacks();
      },
      () => { this.locationDenied = true; }
    );
  }

  loadPacks(): void {
    this.loading.set(true);
    this.error.set(null);

    const filters: PackFilters = {
      lat:         this.userLat  ?? undefined,
      lon:         this.userLon  ?? undefined,
      search:      this.searchInput || undefined,
      categoryId:  this.selectedCategory ?? undefined,
      maxPrice:    this.selectedMaxPrice    ? Number(this.selectedMaxPrice)    : undefined,
      maxDistance: this.selectedMaxDistance ? Number(this.selectedMaxDistance) : undefined,
    };

    this.catalog.getPacks(filters).subscribe({
      next:  packs => { this.packs.set(packs); this.loading.set(false); },
      error: ()    => { this.error.set('No se pudieron cargar los packs. Intentá de nuevo.'); this.loading.set(false); }
    });
  }

  selectCategory(id: number | null): void {
    this.selectedCategory = id;
    this.loadPacks();
  }

  openPack(id: number): void {
    this.router.navigate(['/packs', id]);
  }

  discountPercent(pack: PackListItem): number {
    if (!pack.originalPrice) return 0;
    return Math.round((1 - pack.salePrice / pack.originalPrice) * 100);
  }

  formatTime(t: string): string {
    return t.substring(0, 5);
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }
}
