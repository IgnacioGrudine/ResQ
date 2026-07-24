import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { Order, ConsumerProfile } from '../models/consumer.models';

export interface ReviewRequest {
  rating: number;
  comment?: string;
}

export interface ReviewResponse {
  id: number;
  rating: number;
  comment?: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class ConsumerService {
  private readonly http = inject(HttpClient);

  /**
   * Fires whenever an order's state changes somewhere in the app (e.g. a review submitted
   * via the global auto-review modal in ConsumerLayoutComponent). Components that keep their
   * own local copy of the order list (like OrdersComponent) should refetch when this fires,
   * since Angular won't otherwise know their already-loaded data went stale.
   */
  private readonly ordersChanged = new Subject<void>();
  readonly ordersChanged$ = this.ordersChanged.asObservable();
  notifyOrdersChanged(): void { this.ordersChanged.next(); }

  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>('/api/consumers/me/orders');
  }

  getProfile(): Observable<ConsumerProfile> {
    return this.http.get<ConsumerProfile>('/api/consumers/me');
  }

  updateProfile(data: { firstName: string; lastName: string; phoneNumber: string }): Observable<ConsumerProfile> {
    return this.http.put<ConsumerProfile>('/api/consumers/me', data);
  }

  submitReview(orderId: number, payload: ReviewRequest): Observable<ReviewResponse> {
    return this.http.post<ReviewResponse>(`/api/consumers/me/orders/${orderId}/review`, payload);
  }
}
