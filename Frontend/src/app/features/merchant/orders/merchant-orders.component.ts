import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { MerchantOrder, MerchantOrderStatus } from '../../../core/models/merchant.models';
import { LucideClipboardList, LucideCheck, LucideTicket } from '@lucide/angular';

type FilterTab = 'all' | 'pending' | 'delivered' | 'cancelled';

@Component({
  selector: 'app-merchant-orders',
  standalone: true,
  imports: [DecimalPipe, FormsModule, LucideClipboardList, LucideCheck, LucideTicket],
  templateUrl: './merchant-orders.component.html'
})
export class MerchantOrdersComponent implements OnInit {
  private readonly merchant = inject(MerchantService);

  readonly orders  = signal<MerchantOrder[]>([]);
  readonly loading = signal(true);
  activeTab: FilterTab = 'all';

  // Confirm-pickup box state
  codeInput = '';
  readonly confirming   = signal(false);
  readonly confirmMsg   = signal<string | null>(null);
  readonly confirmOk    = signal(false);

  // Per-row confirm spinner
  readonly rowConfirming = signal<number | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.merchant.getOrders().subscribe({
      next:  o  => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  get filteredOrders(): MerchantOrder[] {
    const all = this.orders();
    switch (this.activeTab) {
      case 'pending':   return all.filter(o => o.orderStatus === 'Paid');
      case 'delivered': return all.filter(o => o.orderStatus === 'PickedUp');
      case 'cancelled': return all.filter(o => o.orderStatus === 'Cancelled');
      default:          return all;
    }
  }

  get activeCount(): number {
    return this.orders().filter(o => o.orderStatus === 'Paid').length;
  }

  // Confirm via the top search box
  confirmByCode(): void {
    const code = this.codeInput.trim();
    if (!code) return;

    this.confirming.set(true);
    this.confirmMsg.set(null);

    this.merchant.confirmPickup(code).subscribe({
      next: updated => {
        this.confirming.set(false);
        this.confirmOk.set(true);
        this.confirmMsg.set(`Retiro confirmado: ${updated.consumerName}`);
        this.codeInput = '';
        this.applyUpdate(updated);
        setTimeout(() => this.confirmMsg.set(null), 4000);
      },
      error: err => {
        this.confirming.set(false);
        this.confirmOk.set(false);
        this.confirmMsg.set(err.error?.detail ?? 'No se pudo confirmar el retiro.');
      }
    });
  }

  // Confirm via the inline button on a Paid order
  confirmRow(o: MerchantOrder): void {
    this.rowConfirming.set(o.id);
    this.merchant.confirmPickup(o.pickupCode).subscribe({
      next: updated => { this.rowConfirming.set(null); this.applyUpdate(updated); },
      error: () => this.rowConfirming.set(null)
    });
  }

  private applyUpdate(updated: MerchantOrder): void {
    this.orders.update(list => list.map(x => x.id === updated.id ? updated : x));
  }

  statusLabel(status: MerchantOrderStatus): string {
    const map: Record<MerchantOrderStatus, string> = {
      Pending:   'PENDIENTE',
      Paid:      'POR RETIRAR',
      PickedUp:  'ENTREGADO',
      Cancelled: 'CANCELADO',
    };
    return map[status];
  }

  statusClasses(status: MerchantOrderStatus): string {
    const map: Record<MerchantOrderStatus, string> = {
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
