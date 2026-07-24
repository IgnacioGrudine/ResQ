import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ConsumerService } from '../../../core/services/consumer.service';
import { OrderService } from '../../../core/services/order.service';
import { Order, OrderStatus } from '../../../core/models/consumer.models';
import { LucideShoppingBag, LucideChevronLeft, LucideChevronRight, LucideStar, LucideX, LucideTriangleAlert } from '@lucide/angular';

type FilterTab = 'active' | 'cancelled' | 'completed' | 'all';

const PAGE_SIZE = 5;

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [DecimalPipe, FormsModule, LucideShoppingBag, LucideChevronLeft, LucideChevronRight, LucideStar, LucideX, LucideTriangleAlert],
  templateUrl: './orders.component.html'
})
export class OrdersComponent implements OnInit {
  private readonly consumer = inject(ConsumerService);
  private readonly orderService = inject(OrderService);

  readonly orders  = signal<Order[]>([]);
  readonly loading = signal(true);

  // Review modal state
  reviewOrderId: number | null = null;
  reviewRating   = 0;
  hoverRating    = 0;
  reviewComment  = '';
  readonly submitting = signal(false);

  // Cancel modal state
  cancelOrderId: number | null = null;
  readonly cancelling  = signal(false);
  readonly cancelError = signal('');

  private _activeTab: FilterTab = 'active';
  page = 1;

  get activeTab(): FilterTab { return this._activeTab; }

  ngOnInit(): void {
    this.consumer.getOrders().subscribe({
      // Pending orders are checkouts that never got a confirmed payment (abandoned or
      // rejected) — nothing the consumer can act on, so they're excluded everywhere here.
      next:  orders => { this.orders.set(orders.filter(o => o.orderStatus !== 'Pending')); this.loading.set(false); },
      error: ()     => this.loading.set(false)
    });
  }

  setTab(tab: FilterTab): void {
    this._activeTab = tab;
    this.page = 1;
  }

  get filteredOrders(): Order[] {
    const all = this.orders();
    switch (this._activeTab) {
      case 'active':    return all.filter(o => o.orderStatus === 'Paid');
      case 'completed': return all.filter(o => o.orderStatus === 'PickedUp');
      case 'cancelled': return all.filter(o => o.orderStatus === 'Cancelled');
      default:          return all;
    }
  }

  get pagedOrders(): Order[] {
    const start = (this.page - 1) * PAGE_SIZE;
    return this.filteredOrders.slice(start, start + PAGE_SIZE);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredOrders.length / PAGE_SIZE));
  }

  setPage(p: number): void {
    if (p >= 1 && p <= this.totalPages) this.page = p;
  }

  statusLabel(status: OrderStatus): string {
    const map: Record<OrderStatus, string> = {
      Pending:   'PENDIENTE',
      Paid:      'PAGADO',
      PickedUp:  'RETIRADO',
      Cancelled: 'CANCELADO',
    };
    return map[status];
  }

  statusClasses(status: OrderStatus): string {
    const map: Record<OrderStatus, string> = {
      Pending:   'bg-yellow-50 text-yellow-700 border border-yellow-200',
      Paid:      'bg-blue-50 text-blue-700 border border-blue-200',
      PickedUp:  'bg-lime/60 text-hunter border border-fern/30',
      Cancelled: 'bg-red-50 text-red-600 border border-red-200',
    };
    return map[status];
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  // ── Review modal ─────────────────────────────────────────────────────────

  openReviewModal(orderId: number): void {
    this.reviewOrderId = orderId;
    this.reviewRating  = 0;
    this.hoverRating   = 0;
    this.reviewComment = '';
    this.submitting.set(false);
  }

  closeReviewModal(): void {
    this.reviewOrderId = null;
  }

  setRating(star: number): void {
    this.reviewRating = star;
  }

  submitReview(): void {
    if (!this.reviewOrderId || this.reviewRating === 0 || this.submitting()) return;

    this.submitting.set(true);
    this.consumer.submitReview(this.reviewOrderId, {
      rating:  this.reviewRating,
      comment: this.reviewComment.trim() || undefined
    }).subscribe({
      next: () => {
        this.orders.update(list =>
          list.map(o => o.id === this.reviewOrderId ? { ...o, hasReview: true } : o)
        );
        this.closeReviewModal();
      },
      error: () => { this.submitting.set(false); }
    });
  }

  // ── Cancel modal ─────────────────────────────────────────────────────────

  openCancelModal(orderId: number): void {
    this.cancelOrderId = orderId;
    this.cancelling.set(false);
    this.cancelError.set('');
  }

  closeCancelModal(): void {
    this.cancelOrderId = null;
  }

  confirmCancel(): void {
    if (!this.cancelOrderId || this.cancelling()) return;

    this.cancelling.set(true);
    this.cancelError.set('');

    this.orderService.cancelOrder(this.cancelOrderId).subscribe({
      next: () => {
        this.orders.update(list =>
          list.map(o => o.id === this.cancelOrderId ? { ...o, orderStatus: 'Cancelled' as OrderStatus } : o)
        );
        this.closeCancelModal();
      },
      error: (err: HttpErrorResponse) => {
        this.cancelling.set(false);
        this.cancelError.set(err.error?.detail ?? 'No se pudo cancelar la orden. Intentá de nuevo.');
      }
    });
  }
}
