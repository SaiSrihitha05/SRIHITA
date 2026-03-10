import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-admin-side-bar',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './admin-side-bar.html',
  styleUrl: './admin-side-bar.css',
})
export class AdminSideBar {
  navLinks = [
    { label: 'Dashboard', icon: '📊', path: '/admin-dashboard' },
    { label: 'Insurance Plans', icon: '📋', path: '/admin-dashboard/plans' },
    { label: 'Policies', icon: '🛡️', path: '/admin-dashboard/policies' },
    { label: 'Loan Management', icon: '🏦', path: '/admin-dashboard/loans' },
    { label: 'Claims', icon: '💰', path: '/admin-dashboard/claims' },
    { label: 'Manage Users', icon: '👥', path: '/admin-dashboard/users' },
    { label: 'Profile', icon: '👤', path: '/admin-dashboard/profile' }
  ];
}
