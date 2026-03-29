import { Component, inject, signal, ViewChild, ElementRef, AfterViewChecked, OnInit, OnDestroy, ChangeDetectorRef, DestroyRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, ChatResponse, EscalationTarget, ChatSenderType, SuggestedQuestion } from '../../services/chat-service';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../services/auth-service';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

interface Message {
  text: string;
  senderType: ChatSenderType;
  messageType?: 'Text' | 'PlanLink';
  planId?: number;
  planName?: string;
  planUrl?: string;
  intent?: string;
  escalationTarget?: EscalationTarget;
  action?: { 
    type: string; 
    planId?: number; 
    planName?: string; 
    url: string; 
  };
  suggestedQuestions?: SuggestedQuestion[];
  timestamp: Date;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './chatbot.html',
  styleUrl: './chatbot.css'
})
export class Chatbot implements AfterViewChecked, OnInit {
  private chatService = inject(ChatService);
  private authService = inject(AuthService);
  private sanitizer = inject(DomSanitizer);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);
  private ngZone = inject(NgZone);
  private router = inject(Router);
  
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  isOpen = signal(false);
  userInput = signal('');
  messages = signal<Message[]>([]);
  isLoading = signal(false);
  isTyping = signal(false);
  isChatClosed = signal(false);
  isAgentConnected = signal(false);
  isOfficerConnected = signal(false);
  sessionId = signal('');
  pendingAction: any = null;
  chipsSent = signal(false);

  // Predefined starter questions
  starterQuestions = [
    { text: 'View Term Plans', prompt: 'Tell me about available term insurance plans' },
    { text: 'Health Insurance', prompt: 'What health insurance plans do you have?' },
    { text: 'Investment Plans', prompt: 'Show me some investment and savings plans' },
    { text: 'Check Claim Status', prompt: 'How do I check my claim status?' },
    { text: 'Talk to Agent', prompt: 'I want to talk to an agent' }
  ];

  async ngOnInit() {
    this.initSession();
    
    // Check history first
    this.chatService.getHistory(this.sessionId()).subscribe({
      next: (history: any[]) => {
        if (history.length === 0) {
          this.loadWelcomeMessage();
        } else {
          this.loadHistory(history);
        }
      }
    });
    
    // Wait for connection BEFORE joining
    await this.chatService.waitForConnection();
    
    // Join SignalR Session
    this.chatService.joinSession(this.sessionId());

    // Listen for Real-time Messages
    this.chatService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.ngZone.run(() => {
          const msg: Message = {
            text: data.message || data.Message,
            senderType: (data.senderType || data.SenderType) as ChatSenderType,
            intent: data.intent || data.Intent,
            timestamp: new Date(data.createdAt || data.CreatedAt || new Date())
          };

          // If we have a pending action and this is an AI message, attach it
          if (this.pendingAction && msg.senderType === ChatSenderType.AI) {
            msg.action = this.pendingAction;
            this.pendingAction = null;
          }

          // Avoid duplicates
          const isDuplicate = this.messages().some(m => 
            m.text === msg.text && 
            Math.abs(m.timestamp.getTime() - msg.timestamp.getTime()) < 1000
          );

          if (!isDuplicate) {
            this.messages.update(ms => [...ms, msg]);
            this.isTyping.set(false);
            this.isLoading.set(false);
            
            // 🔥 Update connection state based on message sender
            if (msg.senderType === ChatSenderType.Agent) {
              this.isAgentConnected.set(true);
              this.isOfficerConnected.set(false);
            } else if (msg.senderType === ChatSenderType.Officer) {
              this.isOfficerConnected.set(true);
              this.isAgentConnected.set(false);
            }

            // NEW: Reset chips if AI sends new ones
            if (msg.senderType === ChatSenderType.AI && (data.suggestedQuestions || data.SuggestedQuestions)) {
              this.chipsSent.set(false);
            }
          }
        });
      });

    // Listen for Real-time Plan Links
    this.chatService.planLinkReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.ngZone.run(() => {
          if (this.sessionId() === data.sessionId || this.sessionId() === data.SessionId) {
            const msg: Message = {
              text: data.message || data.Message,
              senderType: ChatSenderType.Agent,
              messageType: 'PlanLink',
              planId: data.planId || data.PlanId,
              planName: data.planName || data.PlanName,
              planUrl: data.planUrl || data.PlanUrl,
              timestamp: new Date(data.createdAt || data.CreatedAt || new Date())
            };
            
            this.messages.update(ms => [...ms, msg]);
            this.isAgentConnected.set(true); // Agent sent a plan link
            this.isTyping.set(false);
            this.isLoading.set(false);
            this.scrollToBottom();
          }
        });
      });

    // Listen for Session Closure
    this.chatService.chatClosed$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.ngZone.run(() => {
          if (data.sessionId === this.sessionId()) {
            // Only hard-close the customer UI if isFullClose is true.
            // If false, it means the human left but AI is still available.
            if (data.isFullClose) {
              this.isChatClosed.set(true);
            } else {
              // 🔥 Reset human assignment state
              this.isAgentConnected.set(false);
              this.isOfficerConnected.set(false);
              this.isTyping.set(false);
              this.isLoading.set(false);
            }
          }
        });
      });
  }

  private initSession() {
    let storedId = localStorage.getItem('chat_session_id');
    if (!storedId) {
      storedId = crypto.randomUUID();
      localStorage.setItem('chat_session_id', storedId);
    }
    this.sessionId.set(storedId);
  }



  loadWelcomeMessage() {
    this.chatService.getWelcome(this.sessionId()).subscribe({
      next: (res) => {
        this.messages.set([{
          text: res.response,
          senderType: ChatSenderType.AI,
          suggestedQuestions: res.suggestedQuestions,
          timestamp: new Date()
        }]);
        this.scrollToBottom();
      }
    });
  }

  loadHistory(history: any[]) {
    const mapped: Message[] = history.map(m => ({
      text: m.message,
      senderType: m.senderType as ChatSenderType,
      messageType: m.messageType as any,
      planId: m.linkedPlanId,
      planName: m.linkedPlanName,
      planUrl: m.linkedPlanUrl,
      intent: m.intent,
      timestamp: new Date(m.createdAt)
    }));
    
    // 🔥 Initial State Recognition from History
    const lastStaffMsg = [...mapped].reverse().find(m => 
      m.senderType === ChatSenderType.Agent || m.senderType === ChatSenderType.Officer
    );
    const lastSystemMsg = [...mapped].reverse().find(m => 
      m.senderType === ChatSenderType.System && (m.text.includes('left') || m.text.includes('🔕'))
    );

    if (lastStaffMsg) {
      const staffIdx = mapped.indexOf(lastStaffMsg);
      const systemIdx = lastSystemMsg ? mapped.indexOf(lastSystemMsg) : -1;

      // Only mark connected if there isn't a "left" message AFTER the last staff message
      if (systemIdx < staffIdx) {
        if (lastStaffMsg.senderType === ChatSenderType.Agent) this.isAgentConnected.set(true);
        if (lastStaffMsg.senderType === ChatSenderType.Officer) this.isOfficerConnected.set(true);
      }
    }

    this.messages.set(mapped);
    this.scrollToBottom();
  }

  sendQuickReply(message: string) {
    if (this.chipsSent() || this.isChatClosed()) return;
    this.chipsSent.set(true);
    this.userInput.set(message);
    this.sendMessage();
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  toggleChat() {
    this.isOpen.update(v => !v);
  }

  handleQuickAction(question: string) {
    if (this.isChatClosed()) return;
    this.userInput.set(question);
    this.sendMessage();
  }

  sendMessage() {
    const text = this.userInput().trim();
    if (!text || this.isChatClosed()) return;

    this.userInput.set('');
    this.isTyping.set(true);
    this.isLoading.set(true);

    // NEW: Push user message immediately for responsiveness
    const userMsg: Message = {
      text: text,
      senderType: ChatSenderType.Customer,
      timestamp: new Date()
    };
    this.messages.update(ms => [...ms, userMsg]);

    // Build context from last 6 messages
    const context = this.messages()
        .slice(-6)
        .map(m => `${m.senderType}: ${m.text}`);

    this.chatService.sendMessage(text, this.sessionId(), context).subscribe({
      next: (res: any) => {
        if (res.action) {
          this.pendingAction = {
            ...res.action,
            planId: res.action.planId ? Number(res.action.planId) : undefined
          };
        }

        // 🔥 Fallback: Display response from HTTP if SignalR missed it
        if (res.response) {
          const msg: Message = {
            text: res.response,
            senderType: ChatSenderType.AI,
            intent: res.intent,
            suggestedQuestions: res.suggestedQuestions,
            timestamp: new Date()
          };

          const isDuplicate = this.messages().some(m => 
            m.text === msg.text && 
            Math.abs(m.timestamp.getTime() - msg.timestamp.getTime()) < 1000
          );

          if (!isDuplicate) {
            this.messages.update(ms => [...ms, msg]);
          }
        }
        
        this.isTyping.set(false);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isTyping.set(false);
        this.isLoading.set(false);
        console.error('Chat error:', err);
        this.messages.update(ms => [...ms, {
          text: 'Sorry, I am having trouble connecting right now. Please try again later.',
          senderType: ChatSenderType.System,
          timestamp: new Date()
        }]);
      }
    });
  }

  formatText(text: string): SafeHtml {
    if (!text) return '';
    // Bold
    let formatted = text.replace(/\*\*(.*?)\*\*/g, '<b>$1</b>');
    // Lines
    formatted = formatted.replace(/\n/g, '<br>');
    // Markdown Links: [text](url) -> <a href="url" class="text-pink-100 font-black underline hover:text-white transition-colors">text</a>
    formatted = formatted.replace(/\[(.*?)\]\((.*?)\)/g, '<a href="$2" class="text-pink-100 font-black underline hover:text-white transition-colors">$1</a>');
    
    return this.sanitizer.bypassSecurityTrustHtml(formatted);
  }

  get ChatSenderType() {
    return ChatSenderType;
  }

  typingLabel(): string {
    if (this.isAgentConnected()) return 'Agent is typing...';
    if (this.isOfficerConnected()) return 'Claims Officer is typing...';
    return 'Hartford AI is typing...';
  }

  private scrollToBottom(): void {
    try {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch(err) { }
  }
}
