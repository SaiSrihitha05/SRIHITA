import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-generic-error',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="error-page">
      <div class="error-card">
        <div class="error-code">Error</div>
        <div class="error-icon">⚠️</div>
        <h1 class="error-title">Something went wrong</h1>
        <p class="error-message">
          An unexpected error occurred. Please try again later or contact support.
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
      background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
      font-family: 'Inter', sans-serif;
    }
    .error-card {
      background: white;
      border-radius: 20px;
      padding: 60px 50px;
      text-align: center;
      box-shadow: 0 20px 60px rgba(0,0,0,0.1);
      max-width: 460px;
      width: 90%;
      animation: fadeIn 0.5s ease-out;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: scale(0.95); }
      to   { opacity: 1; transform: scale(1); }
    }
    .error-code {
      font-size: 3rem;
      font-weight: 800;
      color: #75013f;
      margin-bottom: 10px;
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
      background: #75013f;
      color: white;
      border: none;
      padding: 14px 36px;
      border-radius: 50px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }
    .btn-home:hover {
      background: #fe3082;
      transform: translateY(-2px);
      box-shadow: 0 8px 24px rgba(117,1,63,0.3);
    }
  `]
})
export class GenericError {
    constructor(private router: Router) { }

    goHome() {
        this.router.navigate(['/']);
    }
}
