import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../services/notification-service';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-center.html'
})
export class NotificationCenter implements OnInit {
  private notifyService = inject(NotificationService);
  private cdr = inject(ChangeDetectorRef);
  notifications: any[] = [];
  unreadCount = 0;

  ngOnInit() {
    this.loadNotifications();
  }

  loadNotifications() {
    this.notifyService.getMyNotifications().subscribe(data => {
      this.notifications = data;
      this.unreadCount = data.filter(n => !n.isRead).length;
      this.cdr.detectChanges();
    });
  }

  markRead(id: number) {
    this.notifyService.markAsRead(id).subscribe(() => {
      this.loadNotifications();
      this.cdr.detectChanges();
    });
  }

  markAllRead() {
    this.notifyService.markAllAsRead().subscribe(() => {
      this.loadNotifications();
      this.cdr.detectChanges();
    });
  }

  getIcon(type: string): string {
    switch (type) {
      case 'ClaimStatusUpdate': return '📑';
      case 'PaymentReminder': return '💰';
      default: return '🔔';
    }
  }
}