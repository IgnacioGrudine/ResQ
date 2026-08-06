import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  LucideLeaf, LucideHome, LucideShoppingBag, LucideUser, LucideLogOut, LucideStar, LucideX,
  LucideMap, LucideChevronLeft, LucideChevronRight
} from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';
import { ConsumerService } from '../../core/services/consumer.service';
import { Order } from '../../core/models/consumer.models';

const SIDEBAR_COLLAPSED_KEY = 'resq.consumer.sidebarCollapsed';

@Component({
  selector: 'app-consumer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule,
            LucideLeaf, LucideHome, LucideShoppingBag, LucideUser, LucideLogOut, LucideStar, LucideX,
            LucideMap, LucideChevronLeft, LucideChevronRight],
  templateUrl: './consumer-layout.component.html'
})
export class ConsumerLayoutComponent implements OnInit {
  private readonly auth       = inject(AuthService);
  private readonly consumer   = inject(ConsumerService);
  private readonly destroyRef = inject(DestroyRef);

  // Queue of orders awaiting a review
  private pendingQueue: Order[] = [];

  readonly pendingOrder = signal<Order | null>(null);

  /** Desktop sidebar collapsed to icon-only mode. Persisted across sessions. */
  readonly sidebarCollapsed = signal(localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === '1');

  reviewRating  = 0;
  hoverRating   = 0;
  reviewComment = '';
  submitting    = false;

  ngOnInit(): void {
    this.loadPendingQueue();

    // A review can also be submitted directly from "Mis Órdenes" — refresh this queue when
    // that happens so we never re-prompt for an order that was just reviewed elsewhere.
    this.consumer.ordersChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadPendingQueue());
  }

  private loadPendingQueue(): void {
    this.consumer.getOrders().subscribe({
      next: orders => {
        const currentId = this.pendingOrder()?.id;
        this.pendingQueue = orders.filter(o =>
          o.orderStatus === 'PickedUp' && !o.hasReview && o.id !== currentId);
        if (!this.pendingOrder()) this.showNext();
      }
    });
  }

  toggleSidebar(): void {
    const next = !this.sidebarCollapsed();
    this.sidebarCollapsed.set(next);
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, next ? '1' : '0');
  }

  private showNext(): void {
    if (this.pendingQueue.length === 0) { this.pendingOrder.set(null); return; }
    const next = this.pendingQueue.shift()!;
    this.pendingOrder.set(next);
    this.reviewRating  = 0;
    this.hoverRating   = 0;
    this.reviewComment = '';
    this.submitting    = false;
  }

  setRating(star: number): void { this.reviewRating = star; }

  dismiss(): void { this.showNext(); }

  ratingLabel(): string {
    const labels: Record<number, string> = {
      1: 'Malo', 2: 'Regular', 3: 'Bueno', 4: 'Muy bueno', 5: 'Excelente'
    };
    return labels[this.hoverRating || this.reviewRating] ?? '';
  }

  submitReview(): void {
    const order = this.pendingOrder();
    if (!order || this.reviewRating === 0 || this.submitting) return;

    this.submitting = true;
    this.consumer.submitReview(order.id, {
      rating:  this.reviewRating,
      comment: this.reviewComment.trim() || undefined
    }).subscribe({
      next: () => {
        // Let any already-loaded page (e.g. "Mis Órdenes") know this order now has a
        // review, since this modal can pop up over any route with its own stale copy.
        this.consumer.notifyOrdersChanged();
        this.showNext();
      },
      error: () => { this.submitting = false; }
    });
  }

  logout(): void { this.auth.logout(); }
}
