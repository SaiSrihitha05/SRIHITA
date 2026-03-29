import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

export interface ChatMessageDto {
    message: string;
    sessionId: string;
    context?: string[];
}

export enum ChatSenderType {
    Customer = 'Customer',
    Agent = 'Agent',
    Officer = 'Officer',
    AI = 'AI',
    System = 'System'
}

export interface EscalationTarget {
    id?: number;
    name: string;
    role: string;
}

export interface SuggestedQuestion {
    label: string;
    message: string;
}

export interface ChatAction {
    type: string;
    planId?: number;
    planName?: string;
    url?: string;
}

export interface ChatResponse {
    response: string;
    intent: string;
    escalationTarget?: EscalationTarget;
    action?: ChatAction;
    suggestedQuestions?: SuggestedQuestion[];
}

export interface HistoryMessage {
    id: number;
    customerId?: number;
    agentId?: number;
    message: string;
    senderType: ChatSenderType;
    messageType?: 'Text' | 'PlanLink';
    planId?: number;
    planName?: string;
    planUrl?: string;
    createdAt: string;
    intent?: string;
}

@Injectable({
    providedIn: 'root',
})
export class ChatService {
    private http = inject(HttpClient);
    private baseUrl = 'https://localhost:7027/api/Chat';
    private hubUrl = 'https://localhost:7027/chatHub';

    private hubConnection?: signalR.HubConnection;
    private messageReceivedSubject = new Subject<any>();
    public messageReceived$ = this.messageReceivedSubject.asObservable();

    private chatClosedSubject = new Subject<{ sessionId: string, isFullClose: boolean }>();
    public chatClosed$ = this.chatClosedSubject.asObservable();

    private planLinkSubject = new Subject<any>();
    public planLinkReceived$ = this.planLinkSubject.asObservable();
 
    public pendingSessionsCount = signal<number>(0);
    private joinedSessions = new Set<string>();
    private connectionPromise?: Promise<void>;

    constructor() {
        this.connectionPromise = this.startConnection();
    }

    private async startConnection() {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        // Register handlers BEFORE starting
        this.hubConnection.on('ReceiveMessage', (data: any) => {
            console.log('SignalR Message Received:', data);
            this.messageReceivedSubject.next(data);
            this.refreshPendingCount();
        });
 
        this.hubConnection.on('ReceivePlanLink', (data: any) => {
            console.log('SignalR PlanLink Received:', data);
            this.planLinkSubject.next(data);
        });
 
        this.hubConnection.on('ChatClosed', (data: any) => {
            console.log('SignalR ChatClosed Received:', data);
            const sessionId = data?.sessionId || data?.SessionId;
            const isFull = data?.isFullClose ?? data?.IsFullClose;
 
            this.chatClosedSubject.next({
                sessionId: sessionId,
                isFullClose: isFull === undefined ? true : (isFull === true || isFull === 'true' || isFull === 1)
            });
            this.refreshPendingCount();
        });
 
        // Rejoin all sessions after reconnect
        this.hubConnection.onreconnected(() => {
            console.log('SignalR Reconnected. Rejoining sessions...');
            this.joinedSessions.forEach(id => this.joinSession(id));
            this.refreshPendingCount();
        });
 
        try {
            await this.hubConnection.start();
            console.log('SignalR connected');
            this.refreshPendingCount();
        } catch (err) {
            console.error('SignalR connection error: ', err);
            throw err;
        }
    }
 
    public refreshPendingCount() {
        const role = localStorage.getItem('role');
        if (role === 'Agent' || role === 'ClaimsOfficer' || role === 'Admin') {
            const endpoint = role === 'ClaimsOfficer' ? 'officer/sessions' : 'agent/sessions';
            this.getSessionsByRole(endpoint).subscribe({
                next: (sessions: any[]) => {
                    this.pendingSessionsCount.set(sessions.length);
                }
            });
        }
    }
 
    private getSessionsByRole(endpoint: string): Observable<any[]> {
        const token = localStorage.getItem('token');
        let headers = new HttpHeaders();
        if (token) {
            headers = headers.set('Authorization', `Bearer ${token}`);
        }
        return this.http.get<any[]>(`${this.baseUrl}/${endpoint}`, { headers });
    }

    public async waitForConnection(): Promise<void> {
        await this.connectionPromise;
    }

    joinSession(sessionId: string) {
        this.joinedSessions.add(sessionId);
        if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
            this.hubConnection.invoke('JoinSession', sessionId)
                .catch(err => console.error('JoinSession error: ', err));
        }
    }

    sendMessage(message: string, sessionId: string, context?: string[]): Observable<ChatResponse> {
        const chatMessage: ChatMessageDto = { message, sessionId, context };
        const token = localStorage.getItem('token');

        let headers = new HttpHeaders();
        if (token) {
            headers = headers.set('Authorization', `Bearer ${token}`);
        }

        return this.http.post<ChatResponse>(this.baseUrl, chatMessage, { headers });
    }

    getHistory(sessionId?: string): Observable<any[]> {
        const token = localStorage.getItem('token');
        let headers = new HttpHeaders();
        if (token) {
            headers = headers.set('Authorization', `Bearer ${token}`);
        }

        const url = sessionId
            ? `${this.baseUrl}/history/${sessionId}`
            : `${this.baseUrl}/history`;

        return this.http.get<any[]>(url, { headers });
    }

    closeSession(sessionId: string): Observable<any> {
        const token = localStorage.getItem('token');
        let headers = new HttpHeaders();
        if (token) {
            headers = headers.set('Authorization', `Bearer ${token}`);
        }
        return this.http.post(`${this.baseUrl}/close/${sessionId}`, {}, { headers });
    }

    getWelcome(sessionId: string): Observable<ChatResponse> {
        return this.http.get<ChatResponse>(`${this.baseUrl}/welcome?sessionId=${sessionId}`);
    }
}
