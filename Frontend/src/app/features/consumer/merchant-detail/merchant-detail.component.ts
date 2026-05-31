import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { DecimalPipe } from '@angular/common';
import { CatalogService } from '../../../core/services/catalog.service';
import { environment } from '../../../../environments/environment';
import { MerchantDetail } from '../../../core/models/catalog.models';
import {
  LucideArrowLeft,
  LucideLeaf,
  LucideMapPin,
  LucidePhone,
  LucideStar,
  LucideClock,
  LucideChevronRight,
  LucideStore
} from '@lucide/angular';

@Component({
  selector: 'app-merchant-detail',
  standalone: true,
  imports: [
    DecimalPipe,
    LucideArrowLeft, LucideLeaf, LucideMapPin, LucidePhone,
    LucideStar, LucideClock, LucideChevronRight, LucideStore
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
