import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideLeaf, LucideHome, LucideShoppingBag, LucideUser, LucideLogOut, LucideStar, LucideX, LucideMap } from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';
import { ConsumerService } from '../../core/services/consumer.service';
import { Order } from '../../core/models/consumer.models';

@Component({
  selector: 'app-consumer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule,
            LucideLeaf, LucideHome, LucideShoppingBag, LucideUser, LucideLogOut, LucideStar, LucideX, LucideMap],
  templateUrl: './consumer-layout.component.html'
})
export class ConsumerLayoutComponent implements OnInit {
  private readonly auth     = inject(AuthService);
  private readonly consumer = inject(ConsumerService);

  // Queue of orders awaiting a review
  private pendingQueue: Order[] = [];

  readonly pendingOrder = signal<Order | null>(null);

  reviewRating  = 0;
  hoverRating   = 0;
  reviewComment = '';
  submitting    = false;

  ngOnInit(): void {
    this.consumer.getOrders().subscribe({
      next: orders => {
        this.pendingQueue = orders.filter(o => o.orderStatus === 'PickedUp' && !o.hasReview);
        this.showNext();
      }
    });
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

  submitReview(): void {
    const order = this.pendingOrder();
    if (!order || this.reviewRating === 0 || this.submitting) return;

    this.submitting = true;
    this.consumer.submitReview(order.id, {
      rating:  this.reviewRating,
      comment: this.reviewComment.trim() || undefined
    }).subscribe({
      next:  () => this.showNext(),
      error: () => { this.submitting = false; }
    });
  }

  logout(): void { this.auth.logout(); }
}
