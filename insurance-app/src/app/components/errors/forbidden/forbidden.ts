import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-forbidden',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="error-page">
      <div class="error-card">
        <div class="error-code">403</div>
        <div class="error-icon">🚫</div>
        <h1 class="error-title">Access Denied</h1>
        <p class="error-message">
          You don't have permission to view this page.
          Please contact your administrator if you believe this is a mistake.
        </p>
        <div class="btn-group">
          <button class="btn-back" (click)="goBack()">Go Back</button>
          <button class="btn-home" (click)="goHome()">Home</button>
        </div>
      </div>
    </div>
  `,
    styles: [`
    .error-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
      font-family: 'Inter', sans-serif;
    }
    .error-card {
      background: white;
      border-radius: 20px;
      padding: 60px 50px;
      text-align: center;
      box-shadow: 0 20px 60px rgba(0,0,0,0.2);
      max-width: 460px;
      width: 90%;
      animation: slideUp 0.5s ease-out;
    }
    @keyframes slideUp {
      from { opacity: 0; transform: translateY(30px); }
      to   { opacity: 1; transform: translateY(0); }
    }
    .error-code {
      font-size: 7rem;
      font-weight: 800;
      color: #f5576c;
      line-height: 1;
      text-shadow: 4px 4px 0px rgba(245,87,108,0.2);
    }
    .error-icon { font-size: 3rem; margin: 10px 0; }
    .error-title {
      font-size: 1.8rem;
      color: #1a1a2e;
      margin: 10px 0;
      font-weight: 700;
    }
    .error-message {
      color: #666;
      font-size: 1rem;
      line-height: 1.6;
      margin: 16px 0 30px;
    }
    .btn-group { display: flex; gap: 12px; justify-content: center; }
    .btn-home, .btn-back {
      padding: 14px 30px;
      border-radius: 50px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      border: none;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .btn-home {
      background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
      color: white;
    }
    .btn-back {
      background: transparent;
      border: 2px solid #f5576c;
      color: #f5576c;
    }
    .btn-home:hover, .btn-back:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 20px rgba(245,87,108,0.3);
    }
  `]
})
export class Forbidden {
    constructor(private router: Router) { }

    goHome() { this.router.navigate(['/']); }
    goBack() { window.history.back(); }
}
