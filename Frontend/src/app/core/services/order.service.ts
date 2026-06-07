import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateOrderRequest, OrderCreatedResponse, OrderSummary } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  createOrder(request: CreateOrderRequest): Observable<OrderCreatedResponse> {
    return this.http.post<OrderCreatedResponse>('/api/orders', request);
  }

  getMyOrders(): Observable<OrderSummary[]> {
    return this.http.get<OrderSummary[]>('/api/orders');
  }

  getOrderById(id: number): Observable<OrderSummary> {
    return this.http.get<OrderSummary>(`/api/orders/${id}`);
  }
}
