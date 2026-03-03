import { Component, OnInit, HostListener, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterModule } from '@angular/router';
import { Router } from '@angular/router';
import { inject } from '@angular/core';
import { NotificationService } from '../../services/notification-service';

import { ThemeToggle } from '../theme-toggle/theme-toggle';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterModule, ThemeToggle],
  templateUrl: './navbar.html'
})
export class Navbar implements OnInit {
  isScrolled: boolean = false;
  isMobileMenuOpen: boolean = false;
  private router = inject(Router);
  private notifyService = inject(NotificationService);
  private cdr = inject(ChangeDetectorRef);
  unreadCount = 0;

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.isScrolled = window.scrollY > 50;
  }

  ngOnInit() {
    // Check initial scroll position
    this.onWindowScroll();
    this.refreshNotifications();
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }
  get isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  get userRole(): string | null {
    return localStorage.getItem('role');
  }

  get userName(): string | null {
    return localStorage.getItem('name');
  }

  get userInitial(): string {
    const name = this.userName;
    if (name) return name.charAt(0).toUpperCase();

    const email = localStorage.getItem('email');
    if (email) return email.charAt(0).toUpperCase();

    return 'U';
  }

  get profileLink(): string {
    const role = this.userRole;
    if (role === 'Admin') return '/admin-dashboard/profile';
    if (role === 'Agent') return '/agent-dashboard/profile';
    if (role === 'ClaimsOfficer') return '/claims-officer-dashboard/profile';
    return '/customer-dashboard/profile';
  }

  logout() {
    localStorage.clear();
    this.router.navigate(['/login']);
  }

  refreshNotifications() {
    if (this.userRole !== 'Customer') return;

    this.notifyService.getMyNotifications().subscribe({
      next: (data) => {
        this.unreadCount = data.filter(n => !n.isRead).length;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Could not fetch notification count', err)
    });
  }
}