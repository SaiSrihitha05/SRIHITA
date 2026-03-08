import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7027/api/Notifications';
  unreadCount = signal<number>(0);

  // Get all notifications for the logged-in user
  getMyNotifications() {
    return this.http.get<any[]>(`${this.baseUrl}`).pipe(
      tap(data => {
        // Update signal whenever notifications are fetched
        this.unreadCount.set(data.filter(n => !n.isRead).length);
      })
    );
  }

  getUnreadCount() {
    return this.http.get<number>(`${this.baseUrl}/unread-count`).pipe(
      tap(count => this.unreadCount.set(count))
    );
  }

  markAsRead(id: number) {
    return this.http.post(`${this.baseUrl}/mark-read/${id}`, {}).pipe(
      tap(() => {
        // Decrement count immediately without re-fetching
        this.unreadCount.update(c => Math.max(0, c - 1));
      })
    );
  }

  markAllAsRead() {
    return this.http.post(`${this.baseUrl}/mark-all-read`, {}).pipe(
      tap(() => this.unreadCount.set(0))  // Reset to 0 immediately
    );
  }
}