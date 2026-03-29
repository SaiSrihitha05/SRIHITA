import { Component, OnInit, ChangeDetectorRef, inject, DestroyRef, ViewChild, ElementRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ChatService, ChatSenderType } from '../../../services/chat-service';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

interface ChatMessage {
  id: number;
  customerId: number | null;
  agentId: number | null;
  message: string;
  senderType: ChatSenderType;
  messageType?: 'Text' | 'PlanLink';
  planId?: number;
  planName?: string;
  planUrl?: string;
  createdAt: string;
}

interface Plan {
  id: number;
  planName: string;
}

interface ChatSessionSummary {
  id: number;
  sessionId: string;
  customerId: number | null;
  agentId: number | null;
  relatedPolicyId: number | null;
  name: string;
  email: string;
  isActive: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-agent-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './agent-chat.html',
  styleUrl: './agent-chat.css'
})
export class AgentChat implements OnInit {
  private chatService = inject(ChatService);
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);
  private ngZone = inject(NgZone);

  sessions: ChatSessionSummary[] = [];
  selectedSession: ChatSessionSummary | null = null;
  messages: ChatMessage[] = [];
  replyMessage: string = '';
  loading: boolean = false;
  availablePlans: Plan[] = [];
  selectedPlanId: number | null = null;
  policyContext: any = null;
  private apiUrl = 'https://localhost:7027/api/Chat';

  @ViewChild('chatContainer') private chatContainer!: ElementRef;

  ngOnInit(): void {
    this.loadSessions();
    this.loadPlans();

    // Listen for Real-time Messages
    this.chatService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.ngZone.run(() => {
          if (this.selectedSession && (data.sessionId === this.selectedSession.sessionId || data.SessionId === this.selectedSession.sessionId)) {
            const msg: ChatMessage = {
              id: 0,
              customerId: data.customerId || data.CustomerId,
              agentId: data.agentId || data.AgentId,
              message: data.message || data.Message,
              senderType: (data.senderType || data.SenderType) as ChatSenderType,
              createdAt: data.createdAt || data.CreatedAt || new Date().toISOString()
            };

            // Avoid duplicates
            const isDuplicate = this.messages.some(m =>
              m.message === msg.message &&
              Math.abs(new Date(m.createdAt).getTime() - new Date(msg.createdAt).getTime()) < 1000
            );

            if (!isDuplicate) {
              this.messages.push(msg);
              this.cdr.detectChanges();
              this.scrollToBottom();
            }
          }
        });
      });

    // Listen for Session Closure
    this.chatService.chatClosed$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.ngZone.run(() => {
          if (this.selectedSession && this.selectedSession.sessionId === data.sessionId) {
            this.selectedSession.isActive = false;
          }

          // Also update the session in the list
          const session = this.sessions.find(s => s.sessionId === data.sessionId);
          if (session) {
            session.isActive = false;
          }
          this.cdr.detectChanges();
        });
      });
  }

  loadSessions() {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.get<ChatSessionSummary[]>(`${this.apiUrl}/agent/sessions`, { headers }).subscribe({
      next: (data) => {
        this.sessions = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load sessions', err)
    });
  }

  loadPlans() {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.http.get<Plan[]>('https://localhost:7027/api/Plans', { headers }).subscribe({
      next: (data) => {
        this.availablePlans = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load plans', err)
    });
  }

  selectSession(session: ChatSessionSummary) {
    this.selectedSession = session;
    this.policyContext = null; // Reset context
    this.chatService.joinSession(session.sessionId);
    this.loadHistory(session.sessionId);

    // FIX: Use session-linked policy if available, otherwise fall back to guessing
    if (session.relatedPolicyId) {
      this.fetchPolicyContextById(session.relatedPolicyId);
    } else {
      this.loadPolicyContext(session.customerId);
    }

    this.cdr.detectChanges();
  }

  fetchPolicyContextById(policyId: number) {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.http.get(`${this.apiUrl}/agent/policy-context-by-id/${policyId}`, { headers }).subscribe({
      next: (data) => {
        this.policyContext = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to fetch policy context by ID', err);
        this.policyContext = null;
        this.cdr.detectChanges();
      }
    });
  }

  loadPolicyContext(customerId: number | null) {
    if (!customerId) {
      this.policyContext = null;
      return;
    }

    const token = localStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    // Get the customer's most recent active policy number
    this.http.get<any[]>(
      `${this.apiUrl}/agent/customer-policies/${customerId}`,
      { headers }
    ).subscribe({
      next: (policies) => {
        const active = policies?.find(p => p.status === 'Active')
          ?? policies?.[0];
        if (active?.policyNumber) {
          this.fetchPolicyContext(active.policyNumber);
        } else {
          this.policyContext = null;
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.policyContext = null;
        this.cdr.detectChanges();
      }
    });
  }

  fetchPolicyContext(policyNumber: string) {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders()
      .set('Authorization', `Bearer ${token}`);

    this.http.get(
      `${this.apiUrl}/agent/policy-context/${policyNumber}`,
      { headers }
    ).subscribe({
      next: (data) => {
        this.policyContext = data;
        this.cdr.detectChanges();
      },
      error: () => {
        this.policyContext = null;
        this.cdr.detectChanges();
      }
    });
  }

  loadHistory(sessionId: string) {
    this.loading = true;
    this.cdr.detectChanges();

    const token = localStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.get<ChatMessage[]>(`${this.apiUrl}/agent/session-history/${sessionId}`, { headers }).subscribe({
      next: (data) => {
        this.messages = data;
        this.loading = false;
        this.cdr.detectChanges();
        this.scrollToBottom();
      },
      error: (err) => {
        console.error('Failed to load history', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  sendReply() {
    if (!this.replyMessage.trim() || !this.selectedSession) return;

    const payload = {
      customerId: this.selectedSession.customerId,
      sessionId: this.selectedSession.sessionId,
      message: this.replyMessage
    };

    this.replyMessage = '';
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.post(`${this.apiUrl}/agent/reply`, payload, { headers }).subscribe({
      next: () => {
        // Message will come back via SignalR
      },
      error: (err) => {
        console.error('Failed to send reply', err);
        this.cdr.detectChanges();
      }
    });
  }

  closeChat() {
    if (!this.selectedSession) return;

    this.chatService.closeSession(this.selectedSession.sessionId).subscribe({
      next: () => {
        this.selectedSession!.isActive = false;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to close chat', err)
    });
  }

  sendPlanLink() {
    if (!this.selectedPlanId || !this.selectedSession) return;

    const payload = {
      sessionId: this.selectedSession.sessionId,
      planId: this.selectedPlanId
    };

    const token = localStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.post(`${this.apiUrl}/agent/send-plan-link`, payload, { headers }).subscribe({
      next: () => {
        this.selectedPlanId = null;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to send plan link', err)
    });
  }

  get ChatSenderType() {
    return ChatSenderType;
  }

  private scrollToBottom() {
    setTimeout(() => {
      if (this.chatContainer) {
        this.chatContainer.nativeElement.scrollTop = this.chatContainer.nativeElement.scrollHeight;
      }
    }, 100);
  }
}
