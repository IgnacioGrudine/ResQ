import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MercadoPagoService {
  private readonly http = inject(HttpClient);

  /**
   * Requests the MP OAuth authorization URL for the authenticated merchant.
   * Sends the current frontend origin so the OAuth callback can return the merchant to
   * this exact origin, where their session cookie is valid (the cookie is host-only, so
   * landing on a different host — e.g. the public tunnel — would drop the session).
   */
  getAuthUrl(): Observable<{ authUrl: string }> {
    const returnOrigin = encodeURIComponent(window.location.origin);
    return this.http.get<{ authUrl: string }>(`/api/merchants/mp/auth-url?returnOrigin=${returnOrigin}`);
  }

  /** Disconnects the merchant's MP account and deactivates their products. */
  disconnect(): Observable<void> {
    return this.http.delete<void>('/api/merchants/mp/disconnect');
  }
}
