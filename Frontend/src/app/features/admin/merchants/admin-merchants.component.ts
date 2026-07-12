import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import {
  AdminMerchantDetail, AdminMerchantListItem, PagedResponse
} from '../../../core/models/admin.models';
import { ResqSelectComponent } from '../../../shared/ui/select/resq-select.component';
import { ResqOptionComponent } from '../../../shared/ui/select/resq-option.component';
import {
  LucideChevronLeft, LucideChevronRight, LucideX, LucideStore, LucideSearch
} from '@lucide/angular';

@Component({
  selector: 'app-admin-merchants',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule, ResqSelectComponent, ResqOptionComponent,
    LucideChevronLeft, LucideChevronRight, LucideX, LucideStore, LucideSearch
  ],
  templateUrl: './admin-merchants.component.html'
})
export class AdminMerchantsComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly page    = signal<PagedResponse<AdminMerchantListItem> | null>(null);
  readonly loading = signal(true);

  // Filters
  activeFilter: '' | 'true' | 'false' = '';
  mpFilter = '';
  currentPage = 1;
  readonly pageSize = 8;

  // Detail modal
  readonly detail        = signal<AdminMerchantDetail | null>(null);
  readonly detailLoading = signal(false);
  readonly busy          = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.admin.getMerchants({
      active:   this.activeFilter === '' ? null : this.activeFilter === 'true',
      mpStatus: this.mpFilter || null,
      page:     this.currentPage,
      pageSize: this.pageSize
    }).subscribe({
      next:  p  => { this.page.set(p); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  applyFilters(): void { this.currentPage = 1; this.load(); }

  setPage(p: number): void {
    const total = this.page()?.totalPages ?? 1;
    if (p >= 1 && p <= total) { this.currentPage = p; this.load(); }
  }

  openDetail(id: number): void {
    this.detailLoading.set(true);
    this.detail.set(null);
    this.admin.getMerchantDetail(id).subscribe({
      next:  d  => { this.detail.set(d); this.detailLoading.set(false); },
      error: () => this.detailLoading.set(false)
    });
  }

  closeDetail(): void { this.detail.set(null); }

  toggleStatus(m: AdminMerchantDetail): void {
    this.busy.set(true);
    this.admin.setMerchantStatus(m.id, !m.isActive).subscribe({
      next: () => {
        this.detail.update(d => d ? { ...d, isActive: !d.isActive } : d);
        this.busy.set(false);
        this.load();
      },
      error: () => this.busy.set(false)
    });
  }

  statusLabel(s: string): string {
    return s === 'Connected' ? 'Conectado' : s === 'TokenExpired' ? 'Token vencido' : 'Desconectado';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
