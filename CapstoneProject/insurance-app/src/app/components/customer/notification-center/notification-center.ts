import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NotificationService } from '../../../services/notification-service';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-center.html'
})
export class NotificationCenter implements OnInit {
  private notifyService = inject(NotificationService);
  private router = inject(Router);
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

  onNotificationClick(note: any) {
    if (!note.isRead) {
      this.markRead(note.id);
    }

    const role = localStorage.getItem('role');
    
    if (note.claimId) {
      if (role === 'Customer') this.router.navigate(['/customer-dashboard/my-claims']);
      else if (role === 'ClaimsOfficer') this.router.navigate(['/claims-officer-dashboard/my-claims']);
      else if (role === 'Admin') this.router.navigate(['/admin-dashboard/claims']);
    } else if (note.policyId) {
      // If customer clicks a loan/policy notification, go to details
      if (role === 'Customer') {
        this.router.navigate(['/customer-dashboard/policy-details', note.policyId]);
      } else if (role === 'Agent') {
        this.router.navigate(['/agent-dashboard/my-policies']);
      } else if (role === 'Admin') {
        this.router.navigate(['/admin-dashboard/policies']);
      }
    } else if (note.paymentId) {
      if (role === 'Customer') this.router.navigate(['/customer-dashboard/payment-history']);
    }
  }

  private getDashboardByRole(role: string | null): string {
    if (role === 'Admin') return 'admin-dashboard';
    if (role === 'Agent') return 'agent-dashboard';
    if (role === 'ClaimsOfficer') return 'claims-officer-dashboard';
    return 'customer-dashboard';
  }

  getIcon(type: string): string {
    switch (type) {
      case 'ClaimStatusUpdate': return '📑';
      case 'PaymentConfirmation': return '💳';
      case 'PremiumReminder': return '⏰';
      case 'PolicyStatusUpdate': return '📄';
      default: return '🔔';
    }
  }
}