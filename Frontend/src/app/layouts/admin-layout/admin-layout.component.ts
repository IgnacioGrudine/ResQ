import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import {
  LucideLeaf,
  LucideLayoutDashboard,
  LucideStore,
  LucideUsers,
  LucideFileText,
  LucideTags,
  LucideLogOut,
  LucideChevronLeft,
  LucideChevronRight
} from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';

const SIDEBAR_COLLAPSED_KEY = 'resq.admin.sidebarCollapsed';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    LucideLeaf, LucideLayoutDashboard, LucideStore, LucideUsers, LucideFileText, LucideTags, LucideLogOut,
    LucideChevronLeft, LucideChevronRight
  ],
  templateUrl: './admin-layout.component.html'
})
export class AdminLayoutComponent {
  private readonly auth = inject(AuthService);

  /** Desktop sidebar collapsed to icon-only mode. Persisted across sessions. */
  readonly sidebarCollapsed = signal(localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1');

  /** Footer copyright year — computed so it never goes stale. */
  readonly currentYear = new Date().getFullYear();

  toggleSidebar(): void {
    const next = !this.sidebarCollapsed();
    this.sidebarCollapsed.set(next);
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, next ? '1' : '0');
  }

  logout(): void { this.auth.logout(); }
}
