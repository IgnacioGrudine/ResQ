import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category, PackListItem, MerchantDetail, MerchantListItem } from '../models/catalog.models';

export interface PackFilters {
  lat?: number;
  lon?: number;
  search?: string;
  categoryId?: number;
  maxPrice?: number;
  maxDistance?: number;
}

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>('/api/catalog/categories');
  }

  getPacks(filters: PackFilters = {}): Observable<PackListItem[]> {
    let params = new HttpParams();
    if (filters.lat != null)         params = params.set('lat', filters.lat);
    if (filters.lon != null)         params = params.set('lon', filters.lon);
    if (filters.search)              params = params.set('search', filters.search);
    if (filters.categoryId != null)  params = params.set('categoryId', filters.categoryId);
    if (filters.maxPrice != null)    params = params.set('maxPrice', filters.maxPrice);
    if (filters.maxDistance != null) params = params.set('maxDistance', filters.maxDistance);
    return this.http.get<PackListItem[]>('/api/catalog/packs', { params });
  }

  getPackById(id: number): Observable<PackListItem> {
    return this.http.get<PackListItem>(`/api/catalog/packs/${id}`);
  }

  getMerchantDetail(id: number): Observable<MerchantDetail> {
    return this.http.get<MerchantDetail>(`/api/catalog/${id}`);
  }

  getCatalog(): Observable<MerchantListItem[]> {
    return this.http.get<MerchantListItem[]>('/api/catalog');
  }
}
