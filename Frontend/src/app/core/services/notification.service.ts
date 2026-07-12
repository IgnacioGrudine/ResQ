import { Injectable, OnDestroy, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MerchantNotification } from '../models/notification.models';

const DISMISSED_KEY = 'resq.merchant.dismissedNotificationIds';

function loadDismissedIds(): Set<number> {
  try {
    const raw = localStorage.getItem(DISMISSED_KEY);
    return raw ? new Set(JSON.parse(raw)) : new Set();
  } catch {
    return new Set();
  }
}

/**
 * Manages the merchant's in-app notifications shown in the panel header bell.
 * Holds the notification list and unread count as signals, polls the unread count
 * on an interval, and exposes operations to refresh the list and mark items read.
 * The JWT interceptor attaches the auth header and `withCredentials`, so plain
 * HttpClient calls are sufficient here.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/notifications';

  /** Polling cadence for the unread badge, in milliseconds. */
  private static readonly POLL_INTERVAL_MS = 30_000;

  private readonly _notifications = signal<MerchantNotification[]>([]);
  private readonly _unreadCount = signal(0);

  /**
   * IDs of read notifications the merchant dismissed from the bell dropdown.
   * Purely a display preference — persisted per-browser via localStorage, never
   * sent to the backend. The underlying notification rows are never deleted.
   */
  private readonly _dismissedIds = signal<Set<number>>(loadDismissedIds());

  /** The merchant's most recent notifications, newest first, minus any the merchant dismissed. */
  readonly notifications = computed(() =>
    this._notifications().filter(n => !this._dismissedIds().has(n.id))
  );
  /** Number of unread notifications, used for the bell badge. */
  readonly unreadCount = this._unreadCount.asReadonly();

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  /**
   * Begins polling the unread count. Safe to call multiple times — only one timer runs.
   * Fetches an initial count immediately so the badge appears without waiting a full interval.
   */
  startPolling(): void {
    if (this.pollTimer !== null) return;
    this.refreshUnreadCount();
    this.pollTimer = setInterval(() => this.refreshUnreadCount(), NotificationService.POLL_INTERVAL_MS);
  }

  /** Stops the unread-count polling timer. */
  stopPolling(): void {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  /** Fetches the unread count and updates the badge signal. */
  refreshUnreadCount(): void {
    this.http.get<number>(`${this.base}/unread-count`).subscribe({
      next: count => this._unreadCount.set(count),
      error: () => {}
    });
  }

  /** Fetches the full notification list (called when the dropdown opens). */
  refreshList(): void {
    this.http.get<MerchantNotification[]>(this.base).subscribe({
      next: items => this._notifications.set(items),
      error: () => {}
    });
  }

  /**
   * Marks a single notification as read, updating the local list and badge optimistically.
   */
  markAsRead(id: number): void {
    const target = this._notifications().find(n => n.id === id);
    if (!target || target.isRead) return;

    this._notifications.update(items =>
      items.map(n => (n.id === id ? { ...n, isRead: true } : n))
    );
    this._unreadCount.update(count => Math.max(0, count - 1));

    this.http.patch<void>(`${this.base}/${id}/read`, {}).subscribe({ error: () => {} });
  }

  /** Marks every notification as read, updating the local state optimistically. */
  markAllAsRead(): void {
    if (this._unreadCount() === 0) return;

    this._notifications.update(items => items.map(n => ({ ...n, isRead: true })));
    this._unreadCount.set(0);

    this.http.patch<void>(`${this.base}/read-all`, {}).subscribe({ error: () => {} });
  }

  /**
   * Hides a read notification from the bell dropdown, in this browser only.
   * No-op for unread notifications — read it first, then dismiss it.
   */
  dismiss(id: number): void {
    const target = this._notifications().find(n => n.id === id);
    if (!target || !target.isRead) return;

    const next = new Set(this._dismissedIds());
    next.add(id);
    this._dismissedIds.set(next);
    localStorage.setItem(DISMISSED_KEY, JSON.stringify([...next]));
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }
}
