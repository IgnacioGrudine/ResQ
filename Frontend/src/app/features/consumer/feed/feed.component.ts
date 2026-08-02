import { Component, OnInit, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs/operators';
import { CatalogService, PackFilters } from '../../../core/services/catalog.service';
import { Category, MerchantListItem, PackListItem } from '../../../core/models/catalog.models';
import { ResqSelectComponent } from '../../../shared/ui/select/resq-select.component';
import { ResqOptionComponent } from '../../../shared/ui/select/resq-option.component';
import { SafeImgDirective } from '../../../shared/directives/safe-img.directive';
import {
  LucideSearch,
  LucideLeaf,
  LucideMapPin,
  LucideClock,
  LucideX,
  LucideRefreshCw,
  LucideStar,
  LucideWheat,
  LucideFish,
  LucideDrumstick,
  LucideUtensilsCrossed,
  LucideSalad,
  LucideHam,
  LucideCakeSlice,
  LucideCookie,
  LucidePizza,
  LucideFlame,
  LucideShoppingCart,
  LucideUtensils,
  LucideSlidersHorizontal,
  LucideChevronRight
} from '@lucide/angular';

interface FilterCategory { id: number | null; name: string; }

/** Maps a category name to its chip icon. Falls back to a generic utensils icon. */
const CATEGORY_ICONS: Record<string, string> = {
  'Panadería':   'wheat',
  'Sushi':       'fish',
  'Rosticería':  'drumstick',
  'Restaurante': 'utensils-crossed',
  'Vegano':      'salad',
  'Fiambrería':  'ham',
  'Pastelería':  'cake-slice',
  'Postres':     'cookie',
  'Pizzería':    'pizza',
  'Parrilla':    'flame',
  'Supermercado':'shopping-cart'
};

/** How many packs to reveal per "Cargar más" click, and initially. */
const PACKS_PAGE_SIZE = 8;

/** Packs closing within this many minutes are eligible for the "Termina pronto" spot. */
const URGENT_THRESHOLD_MINUTES = 120;

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [
    FormsModule, DecimalPipe, NgTemplateOutlet, ResqSelectComponent, ResqOptionComponent, SafeImgDirective,
    LucideSearch, LucideLeaf, LucideMapPin, LucideClock, LucideX, LucideRefreshCw, LucideStar,
    LucideWheat, LucideFish, LucideDrumstick, LucideUtensilsCrossed, LucideSalad,
    LucideHam, LucideCakeSlice, LucideCookie, LucidePizza, LucideFlame, LucideShoppingCart, LucideUtensils,
    LucideSlidersHorizontal, LucideChevronRight
  ],
  templateUrl: './feed.component.html'
})
export class FeedComponent implements OnInit {
  private readonly catalog    = inject(CatalogService);
  private readonly router     = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly searchSubject = new Subject<string>();

  readonly packs      = signal<PackListItem[]>([]);
  readonly categories = signal<FilterCategory[]>([{ id: null, name: 'Todos' }]);
  readonly loading    = signal(false);
  readonly error      = signal<string | null>(null);

  readonly merchants        = signal<MerchantListItem[]>([]);
  readonly merchantsLoading = signal(false);

  searchInput         = '';
  selectedCategory: number | null = null;
  selectedMaxPrice    = '';
  selectedMaxDistance = '';
  userLat: number | null = null;
  userLon: number | null = null;
  locationDenied = false;

  /** Whether the "Filtros" sheet (extra categories + distance + price) is expanded. */
  readonly filtersOpen = signal(false);

  /** How many non-urgent packs are currently revealed in the "Cerca tuyo" grid. */
  readonly visiblePacksCount = signal(PACKS_PAGE_SIZE);

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

  // ── Computed ─────────────────────────────────────────────────────────────────

  /** Fixed set of categories rendered as quick chips; the rest live in the Filtros sheet. */
  private static readonly QUICK_CATEGORY_NAMES = ['Panadería', 'Restaurante', 'Pizzería'];

  readonly quickCategories = computed(() => {
    const all = this.categories();
    return FeedComponent.QUICK_CATEGORY_NAMES
      .map(name => all.find(c => c.name === name))
      .filter((c): c is FilterCategory => c !== undefined);
  });

  /** Every category not already shown as a quick chip, listed in the Filtros sheet. */
  readonly otherCategories = computed(() => {
    const quickIds = new Set(this.quickCategories().map(c => c.id));
    return this.categories().slice(1).filter(c => !quickIds.has(c.id));
  });

  /** First 6 nearby merchants for the compact grid; "Ver todos" leads to the map for the rest. */
  readonly topMerchants = computed(() => this.merchants().slice(0, 6));

  /** The soonest-closing pack within the urgency window, or null if none qualifies. */
  readonly urgentPack = computed<PackListItem | null>(() => {
    let best: PackListItem | null = null;
    let bestMinutes = Infinity;
    for (const p of this.packs()) {
      const mins = this.minutesUntilClose(p.pickupTimeEnd);
      if (mins > 0 && mins <= URGENT_THRESHOLD_MINUTES && mins < bestMinutes) {
        best = p;
        bestMinutes = mins;
      }
    }
    return best;
  });

  /** All packs except the one already featured in "Termina pronto", to avoid duplicates. */
  readonly regularPacks = computed(() => {
    const urgent = this.urgentPack();
    return urgent ? this.packs().filter(p => p.id !== urgent.id) : this.packs();
  });

  readonly visiblePacks = computed(() => this.regularPacks().slice(0, this.visiblePacksCount()));

  readonly hasMorePacks = computed(() => this.visiblePacksCount() < this.regularPacks().length);

  /** Number of active filters not already covered by a quick chip, shown as a badge on "Filtros". */
  readonly activeFilterCount = computed(() => {
    let count = 0;
    if (this.selectedMaxDistance) count++;
    if (this.selectedMaxPrice) count++;
    const quickIds = this.quickCategories().map(c => c.id);
    if (this.selectedCategory !== null && !quickIds.includes(this.selectedCategory)) count++;
    return count;
  });

  ngOnInit(): void {
    // Debounced search: dispara solo cuando hay 0 chars (reset) o 3+
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      filter(v => v.length === 0 || v.length >= 3),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => this.loadPacks());

    this.loadCategories();
    this.loadPacks();
    this.loadMerchants();
    this.requestLocation();
  }

  categoryIcon(name: string): string | null {
    return CATEGORY_ICONS[name] ?? null;
  }

  /**
   * Class string for a category chip. `display` overrides the default 'inline-flex' so
   * responsive-only chips (e.g. 'hidden sm:inline-flex') don't fight an unprefixed base class.
   */
  chipClass(id: number | null, display = 'inline-flex'): string {
    const base = `shrink-0 ${display} items-center gap-1.5 px-3.5 py-2 rounded-full text-xs font-medium transition-all active:scale-90`;
    return base + (this.selectedCategory === id
      ? ' bg-evergreen text-white shadow-sm'
      : ' bg-white text-gray-600 border border-gray-200 hover:border-evergreen hover:text-evergreen');
  }

  loadMerchants(): void {
    this.merchantsLoading.set(true);
    this.catalog.getCatalog().subscribe({
      next: merchants => {
        const sorted = [...merchants]
          .sort((a, b) => b.averageRating - a.averageRating)
          .slice(0, 10);
        this.merchants.set(sorted);
        this.merchantsLoading.set(false);
      },
      error: () => this.merchantsLoading.set(false)
    });
  }

  openMerchant(id: number): void {
    this.router.navigate(['/merchant', id]);
  }

  goToMap(): void {
    this.router.navigate(['/home/mapa']);
  }

  /** True when a merchant has no reviews yet — shown as "Nuevo" instead of a 0-star rating. */
  isNewMerchant(m: MerchantListItem): boolean {
    return m.reviewCount === 0;
  }

  onSearchChange(value: string): void {
    this.searchSubject.next(value);
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
    this.visiblePacksCount.set(PACKS_PAGE_SIZE);

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

  loadMorePacks(): void {
    this.visiblePacksCount.update(n => n + PACKS_PAGE_SIZE);
  }

  selectCategory(id: number | null): void {
    this.selectedCategory = id;
    this.filtersOpen.set(false);
    this.loadPacks();
  }

  toggleFilters(): void {
    this.filtersOpen.update(open => !open);
  }

  clearFilters(): void {
    this.selectedCategory = null;
    this.selectedMaxPrice = '';
    this.selectedMaxDistance = '';
    this.searchInput = '';
    this.filtersOpen.set(false);
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

  /** Minutes from now until the given HH:mm(:ss) time occurs today. Negative if already passed. */
  private minutesUntilClose(pickupTimeEnd: string): number {
    const [h, m] = pickupTimeEnd.split(':').map(Number);
    const now   = new Date();
    const close = new Date(now.getFullYear(), now.getMonth(), now.getDate(), h, m);
    return Math.round((close.getTime() - now.getTime()) / 60000);
  }

  /** Human label for how soon a pack closes, e.g. "Cierra en 40 min" or "Cierra en 1h 30min". */
  closesInLabel(pack: PackListItem): string {
    const mins = this.minutesUntilClose(pack.pickupTimeEnd);
    if (mins <= 0) return 'Cierra pronto';
    if (mins < 60) return `Cierra en ${mins} min`;
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return m === 0 ? `Cierra en ${h}h` : `Cierra en ${h}h ${m}min`;
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }
}
