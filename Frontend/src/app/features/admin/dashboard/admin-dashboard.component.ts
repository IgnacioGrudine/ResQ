import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AdminDashboard, ReportGranularity } from '../../../core/models/admin.models';
import { ResqSelectComponent } from '../../../shared/ui/select/resq-select.component';
import { ResqOptionComponent } from '../../../shared/ui/select/resq-option.component';
import {
  LucideRefreshCw, LucideTrendingUp, LucideStore, LucideUsers,
  LucideStar, LucidePackage, LucideTriangleAlert, LucideInfo
} from '@lucide/angular';

interface Bar { label: string; value: number; orders: number; heightPct: number; }

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule, ResqSelectComponent, ResqOptionComponent,
    LucideRefreshCw, LucideTrendingUp, LucideStore, LucideUsers,
    LucideStar, LucidePackage, LucideTriangleAlert, LucideInfo
  ],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly data    = signal<AdminDashboard | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  from        = isoDaysAgo(30);
  to          = isoToday();
  granularity: ReportGranularity = 'Day';

  /** GMV bars for the activity chart, height-normalized against the period peak. */
  readonly chartBars = computed<Bar[]>(() => {
    const series = this.data()?.activitySeries ?? [];
    const max = Math.max(...series.map(s => s.gmv), 1);
    return series.map(s => ({
      label:     s.label,
      value:     s.gmv,
      orders:    s.orders,
      // Zero-GMV days still get a thin sliver (2%) so they read as "no activity that day"
      // rather than a missing bar that looks like a rendering gap in the chart.
      heightPct: s.gmv === 0 ? 2 : Math.max(Math.round((s.gmv / max) * 100), 6)
    }));
  });

  /**
   * Only every Nth bar gets a visible date label, capped at roughly 10 labels total —
   * with 30+ daily bars every label would collide/overlap, and the chart no longer
   * scrolls horizontally (bars now share the container width), so there's no way to
   * scroll to read a hidden one.
   */
  readonly labelStep = computed(() => Math.max(1, Math.ceil(this.chartBars().length / 10)));

  /** Category rows with width-normalized bars against the top category. */
  readonly categoryBars = computed(() => {
    const cats = this.data()?.categoryDistribution ?? [];
    const max = Math.max(...cats.map(c => c.gmv), 1);
    return cats.map(c => ({ ...c, widthPct: Math.max(Math.round((c.gmv / max) * 100), 4) }));
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.getDashboard({ from: this.from, to: this.to, granularity: this.granularity }).subscribe({
      next:  d  => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('No se pudo cargar el dashboard.'); this.loading.set(false); }
    });
  }

  alertClasses(severity: string): string {
    switch (severity) {
      case 'critical': return 'bg-red-50 border-red-100 text-red-900';
      case 'warning':  return 'bg-amber-50 border-amber-100 text-amber-900';
      default:         return 'bg-blue-50 border-blue-100 text-blue-900';
    }
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
