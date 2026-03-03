import { Injectable,inject } from '@angular/core';
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
    return this.http.post(`${this.baseUrl}/login`, credentials);
  }
  forgotPassword(email: string): Observable<{ token: string }> {
  return this.http.post<{ token: string }>(
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
}

isLoggedIn(): boolean {
  return !!localStorage.getItem('token');
}
}
