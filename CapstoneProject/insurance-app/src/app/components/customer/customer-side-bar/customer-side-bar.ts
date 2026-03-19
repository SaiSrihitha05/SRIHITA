import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-customer-side-bar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './customer-side-bar.html'
})
export class CustomerSideBar {
  navLinks = [
    { label: 'My Dashboard', icon: '🏠', path: '/customer-dashboard' },
    { label: 'Explore Plans', icon: '🔍', path: '/customer-dashboard/explore-plans' },
    { label: 'My Policies', icon: '🛡️', path: '/customer-dashboard/my-policies' },
    { label: 'Payments', icon: '💳', path: '/customer-dashboard/payment-history' },
    { label: 'My Loans', icon: '🏦', path: '/customer-dashboard/my-loans' },
    { label: 'Claims', icon: '📝', path: '/customer-dashboard/my-claims' },
    { label: 'Help Center', icon: '💡', path: '/customer-dashboard/help' },
    { label: 'Profile', icon: '👤', path: '/customer-dashboard/profile' }
  ];
}