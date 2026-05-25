import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ConsumerService } from '../../../core/services/consumer.service';
import { Order, OrderStatus } from '../../../core/models/consumer.models';
import { LucideShoppingBag, LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

type FilterTab = 'active' | 'cancelled' | 'completed' | 'all';

const PAGE_SIZE = 5;

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [DecimalPipe, LucideShoppingBag, LucideChevronLeft, LucideChevronRight],
  templateUrl: './orders.component.html'
})
export class OrdersComponent implements OnInit {
  private readonly consumer = inject(ConsumerService);

  readonly orders  = signal<Order[]>([]);
  readonly loading = signal(true);

  private _activeTab: FilterTab = 'active';
  page = 1;

  get activeTab(): FilterTab { return this._activeTab; }

  ngOnInit(): void {
    this.consumer.getOrders().subscribe({
      next:  orders => { this.orders.set(orders); this.loading.set(false); },
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
}
