import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { MerchantAnalytics, AnalyticsGranularity, ReportFormat } from '../../../core/models/merchant.models';
import {
  LucideRefreshCw, LucideTrendingUp, LucideTrendingDown, LucideStar, LucidePackage
} from '@lucide/angular';

interface Bar { label: string; income: number; orders: number; heightPct: number; }

@Component({
  selector: 'app-merchant-analytics',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule,
    LucideRefreshCw, LucideTrendingUp, LucideTrendingDown, LucideStar, LucidePackage
  ],
  templateUrl: './merchant-analytics.component.html'
})
export class MerchantAnalyticsComponent implements OnInit {
  private readonly merchant = inject(MerchantService);

  readonly data    = signal<MerchantAnalytics | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  from        = isoDaysAgo(30);
  to          = isoToday();
  granularity: AnalyticsGranularity = 'Day';

  readonly chartBars = computed<Bar[]>(() => {
    const series = this.data()?.activitySeries ?? [];
    const max = Math.max(...series.map(s => s.income), 1);
    return series.map(s => ({
      label:     s.day,
      income:    s.income,
      orders:    s.orders,
      heightPct: s.income === 0 ? 0 : Math.max(Math.round((s.income / max) * 100), 6)
    }));
  });

  /** Rating distribution rows with width-normalized bars against the most-frequent rating. */
  readonly ratingRows = computed(() => {
    const dist = this.data()?.ratingDistribution ?? [];
    const max = Math.max(...dist.map(d => d.count), 1);
    return [...dist].sort((a, b) => b.stars - a.stars)
      .map(d => ({ ...d, widthPct: d.count === 0 ? 0 : Math.max(Math.round((d.count / max) * 100), 4) }));
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.merchant.getAnalytics(this.from, this.to, this.granularity).subscribe({
      next:  d  => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('No se pudieron cargar las métricas.'); this.loading.set(false); }
    });
  }

  download(format: ReportFormat): void {
    this.merchant.downloadSalesReport(this.from, this.to, format);
  }
}

function isoToday(): string {
  return new Date().toISOString().slice(0, 10);
}

function isoDaysAgo(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}
