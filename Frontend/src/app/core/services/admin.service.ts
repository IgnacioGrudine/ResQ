import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  AdminDashboard,
  AdminMerchantDetail,
  AdminMerchantListItem,
  AdminUserListItem,
  DashboardFilter,
  MerchantListFilter,
  PagedResponse,
  ReportFormat,
  UserListFilter
} from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/admin';

  // ── Dashboard ──
  getDashboard(filter: DashboardFilter = {}): Observable<AdminDashboard> {
    let params = new HttpParams();
    if (filter.from)        params = params.set('from', filter.from);
    if (filter.to)          params = params.set('to', filter.to);
    if (filter.granularity) params = params.set('granularity', filter.granularity);
    return this.http.get<AdminDashboard>(`${this.base}/dashboard`, { params });
  }

  // ── Merchant management ──
  getMerchants(filter: MerchantListFilter = {}): Observable<PagedResponse<AdminMerchantListItem>> {
    let params = new HttpParams();
    if (filter.active !== null && filter.active !== undefined) params = params.set('active', filter.active);
    if (filter.categoryId)  params = params.set('categoryId', filter.categoryId);
    if (filter.mpStatus)    params = params.set('mpStatus', filter.mpStatus);
    params = params.set('page', filter.page ?? 1).set('pageSize', filter.pageSize ?? 10);
    return this.http.get<PagedResponse<AdminMerchantListItem>>(`${this.base}/merchants`, { params });
  }

  getMerchantDetail(id: number): Observable<AdminMerchantDetail> {
    return this.http.get<AdminMerchantDetail>(`${this.base}/merchants/${id}`);
  }

  setMerchantStatus(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/merchants/${id}/status`, { isActive });
  }

  // ── User management ──
  getUsers(filter: UserListFilter = {}): Observable<PagedResponse<AdminUserListItem>> {
    let params = new HttpParams();
    if (filter.role)   params = params.set('role', filter.role);
    if (filter.active !== null && filter.active !== undefined) params = params.set('active', filter.active);
    params = params.set('page', filter.page ?? 1).set('pageSize', filter.pageSize ?? 10);
    return this.http.get<PagedResponse<AdminUserListItem>>(`${this.base}/users`, { params });
  }

  setUserStatus(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/users/${id}/status`, { isActive });
  }

  // ── Reports (file downloads) ──
  downloadGlobalReport(from: string, to: string, format: ReportFormat): void {
    this.downloadReport(`${this.base}/reports/global`, { from, to, format });
  }

  downloadRankingReport(from: string, to: string, format: ReportFormat): void {
    this.downloadReport(`${this.base}/reports/merchants-ranking`, { from, to, format });
  }

  downloadMerchantReport(merchantId: number, from: string, to: string, format: ReportFormat): void {
    this.downloadReport(`${this.base}/reports/merchants/${merchantId}`, { from, to, format });
  }

  /**
   * Requests a report as a binary blob (so the JWT interceptor attaches the bearer token)
   * and triggers a browser download with the filename returned in Content-Disposition.
   */
  private downloadReport(url: string, query: { from: string; to: string; format: ReportFormat }): void {
    let params = new HttpParams()
      .set('from', query.from)
      .set('to', query.to)
      .set('format', query.format);

    this.http.get(url, { params, responseType: 'blob', observe: 'response' }).subscribe(res => {
      const blob = res.body!;
      const disposition = res.headers.get('Content-Disposition') ?? '';
      const match = /filename="?([^"]+)"?/.exec(disposition);
      const fallback = `resq-reporte.${query.format === 'Excel' ? 'xlsx' : 'pdf'}`;
      triggerDownload(blob, match?.[1] ?? fallback);
    });
  }
}

/** Creates a temporary object URL and clicks a hidden anchor to download the blob. */
function triggerDownload(blob: Blob, filename: string): void {
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(objectUrl);
}
