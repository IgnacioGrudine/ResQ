import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MerchantService } from '../../../core/services/merchant.service';
import { MerchantDashboard } from '../../../core/models/merchant.models';
import {
  LucideClipboardList,
  LucideDollarSign,
  LucidePackage,
  LucideLeaf,
  LucideStar,
  LucideTrendingUp,
  LucideChevronRight,
  LucideCreditCard
} from '@lucide/angular';

@Component({
  selector: 'app-merchant-dashboard',
  standalone: true,
  imports: [
    DecimalPipe, RouterLink,
    LucideClipboardList, LucideDollarSign, LucidePackage, LucideLeaf,
    LucideStar, LucideTrendingUp, LucideChevronRight, LucideCreditCard
  ],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly merchant = inject(MerchantService);

  readonly data    = signal<MerchantDashboard | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  ngOnInit(): void {
    this.merchant.getDashboard().subscribe({
      next:  d  => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('No se pudo cargar el dashboard.'); this.loading.set(false); }
    });
  }

  get mpConnected(): boolean {
    return this.data()?.mpConnectionStatus === 'Connected';
  }

  stars(rating: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }
}
