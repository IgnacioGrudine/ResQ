import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { DatePipe, NgClass } from '@angular/common';
import {
  LucideLeaf,
  LucideLayoutDashboard,
  LucidePackage,
  LucideClipboardList,
  LucideStar,
  LucideStore,
  LucideTrendingUp,
  LucideLogOut,
  LucideBell,
  LucideCheckCheck,
  LucideShoppingBag,
  LucideXCircle,
  LucideChevronDown,
  LucideChevronRight,
  LucideEllipsis,
  LucideX,
  LucideChevronsLeft,
  LucideChevronsRight
} from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';
import { MerchantService } from '../../core/services/merchant.service';
import { NotificationService } from '../../core/services/notification.service';
import { MerchantNotification } from '../../core/models/notification.models';

const SIDEBAR_COLLAPSED_KEY = 'resq.merchant.sidebarCollapsed';

@Component({
  selector: 'app-merchant-layout',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, DatePipe, NgClass,
    LucideLeaf, LucideLayoutDashboard, LucidePackage, LucideClipboardList, LucideStar, LucideStore, LucideTrendingUp, LucideLogOut,
    LucideBell, LucideCheckCheck, LucideShoppingBag, LucideXCircle, LucideChevronDown, LucideChevronRight, LucideEllipsis, LucideX,
    LucideChevronsLeft, LucideChevronsRight
  ],
  templateUrl: './merchant-layout.component.html'
})
export class MerchantLayoutComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly merchant = inject(MerchantService);
  private readonly router = inject(Router);
  readonly notifications = inject(NotificationService);

  /** Desktop sidebar collapsed to icon-only mode. Persisted across sessions. */
  readonly sidebarCollapsed = signal(localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1');

  /** Whether the notification dropdown is currently open. */
  readonly dropdownOpen = signal(false);

  /** Whether the profile menu is currently open. */
  readonly profileMenuOpen = signal(false);

  /** Whether the mobile "Más" bottom sheet (Analytics / Reseñas) is open. */
  readonly moreOpen = signal(false);

  /** The signed-in merchant's public identity, used in the header. */
  readonly businessName = signal<string>('');
  readonly photoUrl = signal<string | null>(null);

  /** Up to two uppercase initials derived from the business name, for the avatar fallback. */
  readonly initials = computed(() => {
    const words = this.businessName().trim().split(/\s+/).filter(Boolean);
    if (words.length === 0) return '';
    if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
    return (words[0][0] + words[1][0]).toUpperCase();
  });

  ngOnInit(): void {
    this.notifications.startPolling();
    this.merchant.getProfile().subscribe({
      next: p => { this.businessName.set(p.businessName); this.photoUrl.set(p.photoUrl ?? null); },
      error: () => {}
    });
  }

  ngOnDestroy(): void {
    this.notifications.stopPolling();
  }

  toggleDropdown(): void {
    const next = !this.dropdownOpen();
    this.dropdownOpen.set(next);
    if (next) {
      this.profileMenuOpen.set(false);
      this.notifications.refreshList();
    }
  }

  closeDropdown(): void {
    this.dropdownOpen.set(false);
  }

  toggleProfileMenu(): void {
    const next = !this.profileMenuOpen();
    this.profileMenuOpen.set(next);
    if (next) this.dropdownOpen.set(false);
  }

  closeProfileMenu(): void {
    this.profileMenuOpen.set(false);
  }

  toggleMore(): void {
    this.moreOpen.set(!this.moreOpen());
  }

  closeMore(): void {
    this.moreOpen.set(false);
  }

  toggleSidebar(): void {
    const next = !this.sidebarCollapsed();
    this.sidebarCollapsed.set(next);
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, next ? '1' : '0');
  }

  onNotificationClick(notification: MerchantNotification): void {
    this.notifications.markAsRead(notification.id);
    this.closeDropdown();
    if (notification.orderId !== null) {
      this.router.navigate(['/panel/orders']);
    }
  }

  markAllAsRead(): void {
    this.notifications.markAllAsRead();
  }

  logout(): void { this.auth.logout(); }
}
