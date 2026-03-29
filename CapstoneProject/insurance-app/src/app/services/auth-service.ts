import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7027/api/Auth';

  register(userData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, userData);
  }

  login(credentials: any): Observable<any> {
    return new Observable(observer => {
      this.http.post(`${this.baseUrl}/login`, credentials).subscribe({
        next: (response: any) => {
          // Perform normal login logic
          if (response.token) {
            localStorage.setItem('token', response.token);
            // ... (rest of storage logic should be here or handled by component)
            
            // Link chat session if it exists
            const sessionId = localStorage.getItem('chat_session_id');
            if (sessionId) {
              this.linkChatSession(sessionId, response.token).subscribe();
            }
          }
          observer.next(response);
          observer.complete();
        },
        error: (err) => observer.error(err)
      });
    });
  }

  private linkChatSession(sessionId: string, token: string): Observable<any> {
    const chatUrl = 'https://localhost:7027/api/Chat/link-session';
    const headers = { 
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    };
    return this.http.post(chatUrl, JSON.stringify(sessionId), { headers });
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post(
      `${this.baseUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/reset-password`, {
      token, newPassword
    });
  }

  getCaptcha(): Observable<{ captchaCode: string }> {
    return this.http.get<{ captchaCode: string }>(
      `${this.baseUrl}/get-captcha`);
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('role');
    localStorage.removeItem('email');
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  getUserRole(): string | null {
    return localStorage.getItem('role');
  }
}
