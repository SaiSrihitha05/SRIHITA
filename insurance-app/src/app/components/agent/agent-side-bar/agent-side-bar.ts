import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-agent-side-bar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './agent-side-bar.html',
  styleUrl: './agent-side-bar.css'
})
export class AgentSideBar {
  navLinks = [
    { label: 'Overview', icon: '📊', path: '/agent-dashboard' },
    { label: 'Assigned Policies', icon: '🛡️', path: '/agent-dashboard/my-policies' },
    { label: 'Profile', icon: '👤', path: '/agent-dashboard/profile' }
  ];
}