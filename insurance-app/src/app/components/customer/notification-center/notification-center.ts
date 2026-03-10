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
  unreadCount = this.notifyService.unreadCount;

  ngOnInit() {
    this.loadNotifications();
  }

  loadNotifications() {
    this.notifyService.getMyNotifications().subscribe(data => {
      this.notifications = data;
      this.cdr.detectChanges();
    });
  }

  markRead(id: number) {
    this.notifyService.markAsRead(id).subscribe(() => {
      this.loadNotifications();
    });
  }

  markAllRead() {
    this.notifyService.markAllAsRead().subscribe(() => {
      this.loadNotifications();
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