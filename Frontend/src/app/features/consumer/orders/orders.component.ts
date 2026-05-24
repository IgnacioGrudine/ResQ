import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ConsumerService } from '../../../core/services/consumer.service';
import { Order, OrderStatus } from '../../../core/models/consumer.models';
import { LucideShoppingBag } from '@lucide/angular';

type FilterTab = 'all' | 'active' | 'completed' | 'cancelled';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [DecimalPipe, LucideShoppingBag],
  templateUrl: './orders.component.html'
})
export class OrdersComponent implements OnInit {
  private readonly consumer = inject(ConsumerService);

  readonly orders  = signal<Order[]>([]);
  readonly loading = signal(true);
  activeTab: FilterTab = 'all';

  ngOnInit(): void {
    this.consumer.getOrders().subscribe({
      next:  orders => { this.orders.set(orders); this.loading.set(false); },
      error: ()     => this.loading.set(false)
    });
  }

  get filteredOrders(): Order[] {
    const all = this.orders();
    switch (this.activeTab) {
      case 'active':    return all.filter(o => o.orderStatus === 'Paid');
      case 'completed': return all.filter(o => o.orderStatus === 'PickedUp');
      case 'cancelled': return all.filter(o => o.orderStatus === 'Cancelled');
      default:          return all;
    }
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
