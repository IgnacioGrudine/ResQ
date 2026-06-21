import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AdminUserListItem, PagedResponse } from '../../../core/models/admin.models';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FormsModule, LucideChevronLeft, LucideChevronRight],
  templateUrl: './admin-users.component.html'
})
export class AdminUsersComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly page    = signal<PagedResponse<AdminUserListItem> | null>(null);
  readonly loading = signal(true);
  readonly busyId  = signal<number | null>(null);

  roleFilter = '';
  activeFilter: '' | 'true' | 'false' = '';
  currentPage = 1;
  readonly pageSize = 10;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.admin.getUsers({
      role:     this.roleFilter || null,
      active:   this.activeFilter === '' ? null : this.activeFilter === 'true',
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

  toggle(u: AdminUserListItem): void {
    if (u.role === 'Admin') return;
    this.busyId.set(u.id);
    this.admin.setUserStatus(u.id, !u.isActive).subscribe({
      next: () => {
        this.page.update(p => p
          ? { ...p, items: p.items.map(x => x.id === u.id ? { ...x, isActive: !x.isActive } : x) }
          : p);
        this.busyId.set(null);
      },
      error: () => this.busyId.set(null)
    });
  }

  roleLabel(role: string): string {
    return role === 'Merchant' ? 'Comercio' : role === 'Consumer' ? 'Consumidor' : 'Admin';
  }

  roleClasses(role: string): string {
    switch (role) {
      case 'Admin':    return 'bg-purple-50 text-purple-700 border-purple-200';
      case 'Merchant': return 'bg-lime/50 text-hunter border-transparent';
      default:         return 'bg-blue-50 text-blue-700 border-blue-200';
    }
  }
}
