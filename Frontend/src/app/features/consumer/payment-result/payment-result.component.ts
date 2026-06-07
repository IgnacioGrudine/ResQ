import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Subscription, interval } from 'rxjs';
import { switchMap, takeWhile } from 'rxjs/operators';
import { OrderService } from '../../../core/services/order.service';
import { OrderSummary } from '../../../core/models/order.models';
import {
  LucideCheckCircle,
  LucideXCircle,
  LucideClock,
  LucideTicket,
  LucideRefreshCw
} from '@lucide/angular';

type ResultType = 'success' | 'failure' | 'pending';
type PageState  = 'polling' | 'paid' | 'error' | 'timeout';

const POLL_INTERVAL_MS = 3000;
const MAX_POLLS        = 40; // ~2 minutes

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [
    DecimalPipe,
    LucideCheckCircle, LucideXCircle, LucideClock,
    LucideTicket, LucideRefreshCw
  ],
  templateUrl: './payment-result.component.html'
})
export class PaymentResultComponent implements OnInit, OnDestroy {
  private readonly route        = inject(ActivatedRoute);
  private readonly router       = inject(Router);
  private readonly orderService = inject(OrderService);

  readonly resultType = signal<ResultType>('success');
  readonly pageState  = signal<PageState>('polling');
  readonly order      = signal<OrderSummary | null>(null);

  private orderId     = 0;
  private pollCount   = 0;
  private pollSub?:   Subscription;

  ngOnInit(): void {
    // Detect result type from current URL path
    const url = this.router.url;
    if (url.includes('exitoso'))  this.resultType.set('success');
    else if (url.includes('fallido'))  this.resultType.set('failure');
    else this.resultType.set('pending');

    this.orderId = Number(this.route.snapshot.queryParamMap.get('orderId') ?? '0');

    if (this.resultType() === 'success' && this.orderId > 0) {
      this.startPolling();
    } else {
      this.pageState.set(this.resultType() === 'failure' ? 'error' : 'error');
    }
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }

  private startPolling(): void {
    this.pollSub = interval(POLL_INTERVAL_MS).pipe(
      switchMap(() => this.orderService.getOrderById(this.orderId)),
      takeWhile((o, index) => {
        if (o.orderStatus === 'Paid') {
          this.order.set(o);
          this.pageState.set('paid');
          return false; // stop
        }
        if (index >= MAX_POLLS) {
          this.pageState.set('timeout');
          return false;
        }
        return true;
      }, true)
    ).subscribe({
      error: () => this.pageState.set('error')
    });
  }

  goHome(): void   { this.router.navigate(['/home']); }
  goOrders(): void { this.router.navigate(['/home/orders']); }
}
