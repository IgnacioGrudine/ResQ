import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { MerchantAnalytics, ReportFormat } from '../../../core/models/merchant.models';
import {
  LucideRefreshCw, LucideTrendingUp, LucideTrendingDown, LucideStar, LucidePackage, LucideSprout
} from '@lucide/angular';

interface Bar { label: string; income: number; orders: number; heightPct: number; showLabel: boolean; }

@Component({
  selector: 'app-merchant-analytics',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule,
    LucideRefreshCw, LucideTrendingUp, LucideTrendingDown, LucideStar, LucidePackage, LucideSprout
  ],
  templateUrl: './merchant-analytics.component.html'
})
export class MerchantAnalyticsComponent implements OnInit {
  private readonly merchant = inject(MerchantService);

  readonly data    = signal<MerchantAnalytics | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  /** Which export format is currently in flight, if any. */
  readonly downloadingFormat = signal<ReportFormat | null>(null);
  readonly downloadError     = signal<string | null>(null);

  from = isoDaysAgo(30);
  to   = isoToday();

  readonly chartBars = computed<Bar[]>(() => {
    const series = this.data()?.activitySeries ?? [];
    const max    = Math.max(...series.map(s => s.income), 1);
    const n      = series.length;
    const step   = n <= 7 ? 1 : n <= 15 ? 2 : n <= 31 ? 5 : 7;
    return series.map((s, i) => ({
      label:     s.day,
      income:    s.income,
      orders:    s.orders,
      // A thin 2% sliver for zero-income days so they read as "no sales that day" instead
      // of a missing bar that looks like a rendering gap (same fix as the admin dashboard).
      heightPct: s.income === 0 ? 2 : Math.max(Math.round((s.income / max) * 100), 6),
      showLabel: i % step === 0
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
    this.merchant.getAnalytics(this.from, this.to).subscribe({
      next:  d  => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('No se pudieron cargar las métricas.'); this.loading.set(false); }
    });
  }

  download(format: ReportFormat): void {
    this.downloadingFormat.set(format);
    this.downloadError.set(null);
    this.merchant.downloadSalesReport(this.from, this.to, format).subscribe({
      next:  () => this.downloadingFormat.set(null),
      error: () => {
        this.downloadingFormat.set(null);
        this.downloadError.set('No se pudo descargar el reporte. Intentá de nuevo.');
      }
    });
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
