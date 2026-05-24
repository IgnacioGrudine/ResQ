import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MerchantService } from '../../../core/services/merchant.service';
import { MerchantDashboard } from '../../../core/models/merchant.models';
import {
  LucideClipboardList,
  LucidePackage,
  LucideLeaf,
  LucideStar,
  LucideTrendingUp,
  LucideChevronRight
} from '@lucide/angular';

interface ChartBar {
  day: string;
  orders: number;
  heightPct: number;
  isToday: boolean;
}

@Component({
  selector: 'app-merchant-dashboard',
  standalone: true,
  imports: [
    DecimalPipe, RouterLink,
    LucideClipboardList, LucidePackage, LucideLeaf,
    LucideStar, LucideTrendingUp, LucideChevronRight
  ],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly merchant = inject(MerchantService);

  readonly data    = signal<MerchantDashboard | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  // ── Computed ─────────────────────────────────────────────────────────────────

  /** Normalized bars for the weekly chart. */
  readonly chartBars = computed<ChartBar[]>(() => {
    const sales = this.data()?.weeklySales ?? [];
    const maxOrders = Math.max(...sales.map(s => s.orders), 1);
    return sales.map((s, i) => ({
      day:       s.day,
      orders:    s.orders,
      heightPct: s.orders === 0 ? 0 : Math.max(Math.round((s.orders / maxOrders) * 100), 12),
      isToday:   i === sales.length - 1
    }));
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.merchant.getDashboard().subscribe({
      next:  d  => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('No se pudo cargar el dashboard.'); this.loading.set(false); }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────────

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Buenos días';
    if (h < 19) return 'Buenas tardes';
    return 'Buenas noches';
  }

  get todayFormatted(): string {
    return new Date().toLocaleDateString('es-AR', {
      weekday: 'long', day: 'numeric', month: 'long'
    });
  }

  readonly starRange = [1, 2, 3, 4, 5];
}
