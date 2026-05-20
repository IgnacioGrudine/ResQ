import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, finalize } from 'rxjs';
import {
  ClientAuthResponse,
  LoginRequest,
  RegisterConsumerRequest,
  RegisterMerchantRequest
} from '../models/auth.models';

interface SessionState {
  accessToken: string | null;
  role: 'Consumer' | 'Merchant' | 'Admin' | null;
  profileId: number | null;
  expiresAt: Date | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _session = signal<SessionState>({
    accessToken: null,
    role: null,
    profileId: null,
    expiresAt: null
  });

  readonly isAuthenticated = computed(() => !!this._session().accessToken);
  readonly role = computed(() => this._session().role);
  readonly accessToken = computed(() => this._session().accessToken);
  readonly profileId = computed(() => this._session().profileId);

  registerConsumer(request: RegisterConsumerRequest): Observable<ClientAuthResponse> {
    return this.http
      .post<ClientAuthResponse>('/api/auth/register/consumer', request, { withCredentials: true })
      .pipe(tap(res => this.setSession(res)));
  }

  registerMerchant(request: RegisterMerchantRequest): Observable<ClientAuthResponse> {
    return this.http
      .post<ClientAuthResponse>('/api/auth/register/merchant', request, { withCredentials: true })
      .pipe(tap(res => this.setSession(res)));
  }

  login(request: LoginRequest): Observable<ClientAuthResponse> {
    return this.http
      .post<ClientAuthResponse>('/api/auth/login', request, { withCredentials: true })
      .pipe(tap(res => this.setSession(res)));
  }

  refresh(): Observable<ClientAuthResponse> {
    return this.http
      .post<ClientAuthResponse>('/api/auth/refresh', {}, { withCredentials: true })
      .pipe(tap(res => this.setSession(res)));
  }

  logout(): void {
    this.http
      .post<void>('/api/auth/logout', {}, { withCredentials: true })
      .pipe(finalize(() => {
        this.clearSession();
        this.router.navigate(['/']);
      }))
      .subscribe({ error: () => {} });
  }

  setSession(response: ClientAuthResponse): void {
    this._session.set({
      accessToken: response.accessToken,
      role: response.role,
      profileId: response.profileId,
      expiresAt: new Date(response.accessTokenExpiresAt)
    });
  }

  clearSession(): void {
    this._session.set({ accessToken: null, role: null, profileId: null, expiresAt: null });
  }
}
