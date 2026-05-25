import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MerchantDashboard,
  MerchantProduct,
  MerchantOrder,
  MerchantReview,
  MerchantProfile,
  ProductPayload,
  UpdateMerchantProfilePayload
} from '../models/merchant.models';

@Injectable({ providedIn: 'root' })
export class MerchantService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/merchants/me';

  // ── Dashboard ──
  getDashboard(): Observable<MerchantDashboard> {
    return this.http.get<MerchantDashboard>(`${this.base}/dashboard`);
  }

  // ── Packs ──
  getProducts(): Observable<MerchantProduct[]> {
    return this.http.get<MerchantProduct[]>(`${this.base}/products`);
  }

  createProduct(payload: ProductPayload): Observable<MerchantProduct> {
    return this.http.post<MerchantProduct>(`${this.base}/products`, payload);
  }

  updateProduct(id: number, payload: ProductPayload): Observable<MerchantProduct> {
    return this.http.put<MerchantProduct>(`${this.base}/products/${id}`, payload);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/products/${id}`);
  }

  toggleProduct(id: number): Observable<MerchantProduct> {
    return this.http.patch<MerchantProduct>(`${this.base}/products/${id}/toggle`, {});
  }

  // ── Orders ──
  getOrders(): Observable<MerchantOrder[]> {
    return this.http.get<MerchantOrder[]>(`${this.base}/orders`);
  }

  confirmPickup(pickupCode: string): Observable<MerchantOrder> {
    return this.http.post<MerchantOrder>(`${this.base}/orders/confirm-pickup`, { pickupCode });
  }

  // ── Reviews ──
  getReviews(): Observable<MerchantReview[]> {
    return this.http.get<MerchantReview[]>(`${this.base}/reviews`);
  }

  // ── Profile ──
  getProfile(): Observable<MerchantProfile> {
    return this.http.get<MerchantProfile>(this.base);
  }

  updateProfile(payload: UpdateMerchantProfilePayload): Observable<MerchantProfile> {
    return this.http.put<MerchantProfile>(this.base, payload);
  }

  uploadPhoto(file: File): Observable<MerchantProfile> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.put<MerchantProfile>(`${this.base}/photo`, formData);
  }

  uploadPackImage(packId: number, file: File): Observable<MerchantProduct> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.put<MerchantProduct>(`${this.base}/products/${packId}/image`, formData);
  }
}
