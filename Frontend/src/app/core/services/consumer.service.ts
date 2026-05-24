import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { Order, ConsumerProfile } from '../models/consumer.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ConsumerService {
  private readonly http = inject(HttpClient);
  private readonly auth  = inject(AuthService);

  // TODO: replace with GET /api/consumer/orders once endpoint is built
  getOrders(): Observable<Order[]> {
    return of([
      {
        id: 1,
        merchantName: 'La Panadería del Centro',
        status: 'Paid',
        pickupCode: 'RSQ-4872',
        totalAmount: 850,
        createdAt: new Date().toISOString(),
        items: [{ packName: 'Pack Sorpresa Dulce', quantity: 1, unitPrice: 850 }]
      },
      {
        id: 2,
        merchantName: 'Sushi Nakamura',
        status: 'PickedUp',
        pickupCode: 'RSQ-3341',
        totalAmount: 1200,
        createdAt: new Date(Date.now() - 86_400_000).toISOString(),
        items: [{ packName: 'Pack Mixto Sushi', quantity: 2, unitPrice: 600 }]
      },
      {
        id: 3,
        merchantName: 'Café Postal',
        status: 'Cancelled',
        totalAmount: 450,
        createdAt: new Date(Date.now() - 172_800_000).toISOString(),
        items: [{ packName: 'Pack Café & Medialunas', quantity: 1, unitPrice: 450 }]
      }
    ]);
  }

  // TODO: replace with GET /api/consumer/profile once endpoint is built
  getProfile(): Observable<ConsumerProfile> {
    return of({
      id: this.auth.profileId() ?? 1,
      firstName: 'Ignacio',
      lastName: 'Grudine',
      email: 'ignaciogrudine0@gmail.com',
      phoneNumber: '+54 351 000-0000',
      totalOrders: 12,
      totalSaved: 4500,
      co2SavedKg: 8.4
    });
  }

  // TODO: replace with PUT /api/consumer/profile once endpoint is built
  updateProfile(data: { firstName: string; lastName: string; phoneNumber: string }): Observable<void> {
    return of(undefined);
  }
}
