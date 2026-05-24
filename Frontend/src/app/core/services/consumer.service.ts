import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Order, ConsumerProfile } from '../models/consumer.models';

@Injectable({ providedIn: 'root' })
export class ConsumerService {
  private readonly http = inject(HttpClient);

  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>('/api/consumers/me/orders');
  }

  getProfile(): Observable<ConsumerProfile> {
    return this.http.get<ConsumerProfile>('/api/consumers/me');
  }

  updateProfile(data: { firstName: string; lastName: string; phoneNumber: string }): Observable<ConsumerProfile> {
    return this.http.put<ConsumerProfile>('/api/consumers/me', data);
  }
}
