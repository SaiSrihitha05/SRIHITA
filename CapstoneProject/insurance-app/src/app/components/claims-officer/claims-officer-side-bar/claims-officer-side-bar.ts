import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ChatService } from '../../../services/chat-service';
import { inject } from '@angular/core';

@Component({
    selector: 'app-claims-officer-side-bar',
    standalone: true,
    imports: [CommonModule, RouterLink, RouterLinkActive],
    templateUrl: './claims-officer-side-bar.html',
    styleUrl: './claims-officer-side-bar.css'
})
export class ClaimsOfficerSideBar {
    private chatService = inject(ChatService);
    pendingCount = this.chatService.pendingSessionsCount;

    navLinks = [
        { label: 'Dashboard', icon: '🏠', path: '/claims-officer-dashboard' },
        { label: 'My Assigned Claims', icon: '💰', path: '/claims-officer-dashboard/my-claims' },
        { label: 'Chat Requests', icon: '💬', path: '/claims-officer-dashboard/chat' }
    ];
}