import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MerchantDashboard,
  MerchantProduct,
  MerchantOrder,
  MerchantReview,
  MerchantProfile,
  ProductPayload,
  UpdateMerchantProfilePayload,
  MerchantAnalytics,
  ReportFormat
} from '../models/merchant.models';

@Injectable({ providedIn: 'root' })
export class MerchantService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/merchants/me';

  // ── Dashboard ──
  getDashboard(): Observable<MerchantDashboard> {
    return this.http.get<MerchantDashboard>(`${this.base}/dashboard`);
  }

  // ── Analytics (filterable) ──
  getAnalytics(from: string, to: string): Observable<MerchantAnalytics> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<MerchantAnalytics>(`${this.base}/analytics`, { params });
  }

  downloadSalesReport(from: string, to: string, format: ReportFormat): void {
    const params = new HttpParams().set('from', from).set('to', to).set('format', format);
    this.http.get(`${this.base}/reports/sales`, { params, responseType: 'blob', observe: 'response' })
      .subscribe(res => {
        const disposition = res.headers.get('Content-Disposition') ?? '';
        const match = /filename="?([^"]+)"?/.exec(disposition);
        const fallback = `resq-ventas.${format === 'Excel' ? 'xlsx' : 'pdf'}`;
        const objectUrl = URL.createObjectURL(res.body!);
        const anchor = document.createElement('a');
        anchor.href = objectUrl;
        anchor.download = match?.[1] ?? fallback;
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(objectUrl);
      });
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
