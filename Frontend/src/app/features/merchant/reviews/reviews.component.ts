import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MerchantService } from '../../../core/services/merchant.service';
import { MerchantReview } from '../../../core/models/merchant.models';
import { LucideStar, LucideMessageSquare } from '@lucide/angular';

@Component({
  selector: 'app-merchant-reviews',
  standalone: true,
  imports: [DecimalPipe, LucideStar, LucideMessageSquare],
  templateUrl: './reviews.component.html'
})
export class ReviewsComponent implements OnInit {
  private readonly merchant = inject(MerchantService);

  readonly reviews = signal<MerchantReview[]>([]);
  readonly loading = signal(true);

  readonly average = computed(() => {
    const list = this.reviews();
    if (!list.length) return 0;
    return Math.round((list.reduce((acc, r) => acc + r.rating, 0) / list.length) * 10) / 10;
  });

  // Distribution from 5 stars down to 1
  readonly distribution = computed(() => {
    const list = this.reviews();
    return [5, 4, 3, 2, 1].map(star => {
      const count = list.filter(r => r.rating === star).length;
      const pct = list.length ? Math.round((count / list.length) * 100) : 0;
      return { star, count, pct };
    });
  });

  ngOnInit(): void {
    this.merchant.getReviews().subscribe({
      next:  r  => { this.reviews.set(r); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  stars(): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
