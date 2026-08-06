import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { DecimalPipe } from '@angular/common';
import { switchMap, map } from 'rxjs';
import { CatalogService } from '../../../core/services/catalog.service';
import { OrderService } from '../../../core/services/order.service';
import { environment } from '../../../../environments/environment';
import { PackListItem, MerchantDetail } from '../../../core/models/catalog.models';
import { SafeImgDirective } from '../../../shared/directives/safe-img.directive';
import {
  LucideArrowLeft,
  LucideLeaf,
  LucideMapPin,
  LucideClock,
  LucidePhone,
  LucideStar,
  LucideChevronRight,
  LucidePackage,
  LucideStore,
  LucideShoppingCart,
  LucideMinus,
  LucidePlus
} from '@lucide/angular';

const SIDEBAR_COLLAPSED_KEY = 'resq.consumer.sidebarCollapsed';

@Component({
  selector: 'app-pack-detail',
  standalone: true,
  imports: [
    DecimalPipe, SafeImgDirective,
    LucideArrowLeft, LucideLeaf, LucideMapPin, LucideClock,
    LucidePhone, LucideStar, LucideChevronRight, LucidePackage, LucideStore,
    LucideShoppingCart, LucideMinus, LucidePlus
  ],
  templateUrl: './pack-detail.component.html'
})
export class PackDetailComponent implements OnInit {
  private readonly route        = inject(ActivatedRoute);
  private readonly catalog      = inject(CatalogService);
  private readonly orderService = inject(OrderService);
  private readonly router       = inject(Router);
  private readonly location     = inject(Location);

  /** Mirrors ConsumerLayoutComponent's own signal so the fixed buy bar doesn't sit under the sidebar. */
  readonly sidebarCollapsed = signal(localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1');

  readonly pack     = signal<PackListItem | null>(null);
  readonly merchant = signal<MerchantDetail | null>(null);
  readonly loading  = signal(true);
  readonly error    = signal<string | null>(null);

  // ── Buy flow ──
  readonly quantity  = signal(1);
  readonly buying    = signal(false);
  readonly buyError  = signal<string | null>(null);

  readonly maxQty = computed(() => Math.min(this.pack()?.stockQuantity ?? 1, 10));

  incrementQty(): void { if (this.quantity() < this.maxQty()) this.quantity.update(q => q + 1); }
  decrementQty(): void { if (this.quantity() > 1) this.quantity.update(q => q - 1); }

  buy(): void {
    const pack = this.pack();
    if (!pack) return;

    this.buying.set(true);
    this.buyError.set(null);

    this.orderService.createOrder({ productId: pack.id, quantity: this.quantity() }).subscribe({
      next: res => {
        // Hard-navigate to MP Checkout Pro
        window.location.href = res.mpCheckoutUrl;
      },
      error: err => {
        this.buying.set(false);
        this.buyError.set(err.error?.detail ?? 'No se pudo crear la orden. Intentá de nuevo.');
      }
    });
  }

  readonly staticMapUrl = computed(() => {
    const m = this.merchant();
    if (!m) return '';
    const { latitude: lat, longitude: lng } = m;
    const key = environment.googleMapsApiKey;
    return `https://maps.googleapis.com/maps/api/staticmap?center=${lat},${lng}&zoom=15&size=600x200&markers=color:red|${lat},${lng}&key=${key}`;
  });

  ngOnInit(): void {
    // React to paramMap (not just the initial snapshot) — Angular reuses this same component
    // instance when navigating from one pack detail to another (e.g. via "Otros packs de..."),
    // since it's the same route config with only the :id changing. Reading the snapshot once
    // in ngOnInit would leave the page stuck on the first pack until a manual reload.
    this.route.paramMap.pipe(
      map(params => Number(params.get('id'))),
      switchMap(id => {
        this.loading.set(true);
        this.error.set(null);
        this.quantity.set(1);

        return this.catalog.getPackById(id).pipe(
          switchMap(pack => {
            this.pack.set(pack);
            return this.catalog.getMerchantDetail(pack.merchantId);
          })
        );
      })
    ).subscribe({
      next:  m  => { this.merchant.set(m); this.loading.set(false); },
      error: () => { this.error.set('No se pudo cargar el pack.'); this.loading.set(false); }
    });
  }

  goBack(): void { this.location.back(); }

  openMerchant(id: number): void { this.router.navigate(['/merchant', id]); }

  openPack(id: number): void { this.router.navigate(['/packs', id]); }

  discountPercent(original: number, sale: number): number {
    if (!original) return 0;
    return Math.round((1 - sale / original) * 100);
  }

  productTypeLabel(type: string): string {
    const t = type?.toLowerCase();
    if (t === 'surprisepack')  return 'Pack Sorpresa';
    if (t === 'explicititem')  return 'Producto exacto';
    return type;
  }

  formatTime(t: string): string { return t.substring(0, 5); }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  stars(rating: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }
}
