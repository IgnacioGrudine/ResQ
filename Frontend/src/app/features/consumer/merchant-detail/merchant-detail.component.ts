import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { DecimalPipe } from '@angular/common';
import { CatalogService } from '../../../core/services/catalog.service';
import { environment } from '../../../../environments/environment';
import { MerchantDetail } from '../../../core/models/catalog.models';
import { SafeImgDirective } from '../../../shared/directives/safe-img.directive';
import {
  LucideArrowLeft,
  LucideLeaf,
  LucideMapPin,
  LucidePhone,
  LucideStar,
  LucideClock,
  LucideStore,
  LucideChevronLeft,
  LucideChevronRight
} from '@lucide/angular';

const PAGE_SIZE = 5;

@Component({
  selector: 'app-merchant-detail',
  standalone: true,
  imports: [
    DecimalPipe, SafeImgDirective,
    LucideArrowLeft, LucideLeaf, LucideMapPin, LucidePhone,
    LucideStar, LucideClock, LucideStore, LucideChevronLeft, LucideChevronRight
  ],
  templateUrl: './merchant-detail.component.html'
})
export class MerchantDetailComponent implements OnInit {
  private readonly route    = inject(ActivatedRoute);
  private readonly catalog  = inject(CatalogService);
  private readonly router   = inject(Router);
  private readonly location = inject(Location);

  readonly merchant = signal<MerchantDetail | null>(null);
  readonly loading  = signal(true);
  readonly error    = signal<string | null>(null);

  readonly selectedRating = signal(0); // 0 = todas
  readonly page = signal(1);

  // Counts per star (5→1), only over ratings that actually have reviews.
  readonly ratingCounts = computed(() => {
    const list = this.merchant()?.recentReviews ?? [];
    return [5, 4, 3, 2, 1]
      .map(star => ({ star, count: list.filter(r => r.rating === star).length }))
      .filter(row => row.count > 0);
  });

  readonly filteredReviews = computed(() => {
    const list = this.merchant()?.recentReviews ?? [];
    const rating = this.selectedRating();
    return rating === 0 ? list : list.filter(r => r.rating === rating);
  });

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredReviews().length / PAGE_SIZE))
  );

  readonly pagedReviews = computed(() => {
    const start = (this.page() - 1) * PAGE_SIZE;
    return this.filteredReviews().slice(start, start + PAGE_SIZE);
  });

  setRatingFilter(rating: number): void {
    this.selectedRating.set(rating);
    this.page.set(1);
  }

  setPage(p: number): void {
    if (p >= 1 && p <= this.totalPages()) this.page.set(p);
  }

  readonly staticMapUrl = computed(() => {
    const m = this.merchant();
    if (!m) return '';
    const { latitude: lat, longitude: lng } = m;
    const key = environment.googleMapsApiKey;
    return `https://maps.googleapis.com/maps/api/staticmap?center=${lat},${lng}&zoom=15&size=600x200&markers=color:red|${lat},${lng}&key=${key}`;
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.catalog.getMerchantDetail(id).subscribe({
      next:  m  => { this.merchant.set(m); this.loading.set(false); },
      error: () => { this.error.set('No se pudo cargar el comercio.'); this.loading.set(false); }
    });
  }

  goBack(): void { this.location.back(); }

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
