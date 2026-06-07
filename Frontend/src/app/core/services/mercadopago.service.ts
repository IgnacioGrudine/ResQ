import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MercadoPagoService {
  private readonly http = inject(HttpClient);

  /** Requests the MP OAuth authorization URL for the authenticated merchant. */
  getAuthUrl(): Observable<{ authUrl: string }> {
    return this.http.get<{ authUrl: string }>('/api/merchants/mp/auth-url');
  }

  /** Disconnects the merchant's MP account and deactivates their products. */
  disconnect(): Observable<void> {
    return this.http.delete<void>('/api/merchants/mp/disconnect');
  }
}
