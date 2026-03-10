import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-not-found',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="error-page">
      <div class="error-card">
        <div class="error-code">404</div>
        <div class="error-icon">🔍</div>
        <h1 class="error-title">Page Not Found</h1>
        <p class="error-message">
          Oops! The page you're looking for doesn't exist or has been moved.
        </p>
        <button class="btn-home" (click)="goHome()">Go to Home</button>
      </div>
    </div>
  `,
    styles: [`
    .error-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
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
      color: #667eea;
      line-height: 1;
      text-shadow: 4px 4px 0px rgba(102,126,234,0.2);
    }
    .error-icon {
      font-size: 3rem;
      margin: 10px 0;
    }
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
    .btn-home {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      padding: 14px 36px;
      border-radius: 50px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .btn-home:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 24px rgba(102,126,234,0.4);
    }
  `]
})
export class NotFound {
    constructor(private router: Router) { }

    goHome() {
        this.router.navigate(['/']);
    }
}
