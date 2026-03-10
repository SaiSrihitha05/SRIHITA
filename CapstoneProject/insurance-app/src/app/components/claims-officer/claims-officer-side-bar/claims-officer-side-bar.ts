import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-claims-officer-side-bar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './claims-officer-side-bar.html',
  styleUrl: './claims-officer-side-bar.css'
})
export class ClaimsOfficerSideBar {
  navLinks = [
    { label: 'Overview', icon: '📉', path: '/claims-officer-dashboard' },
    { label: 'My Assigned Claims', icon: '💰', path: '/claims-officer-dashboard/my-claims' },
    { label: 'Profile', icon: '👤', path: '/claims-officer-dashboard/profile' }
  ];
}