import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ChatService } from '../../../services/chat-service';
import { inject } from '@angular/core';

@Component({
    selector: 'app-agent-side-bar',
    standalone: true,
    imports: [CommonModule, RouterLink, RouterLinkActive],
    templateUrl: './agent-side-bar.html',
    styleUrl: './agent-side-bar.css'
})
export class AgentSideBar {
    private chatService = inject(ChatService);
    pendingCount = this.chatService.pendingSessionsCount;

    navLinks = [
        { label: 'Dashboard', icon: '🏠', path: '/agent-dashboard' },
        { label: 'Assigned Policies', icon: '🛡️', path: '/agent-dashboard/my-policies' },
        { label: 'My Commissions', icon: '💰', path: '/agent-dashboard/commissions' },
        { label: 'Explore Plans', icon: '🎯', path: '/agent-dashboard/explore-plans' },
        { label: 'Chat Requests', icon: '💬', path: '/agent-dashboard/chat' }
    ];
}