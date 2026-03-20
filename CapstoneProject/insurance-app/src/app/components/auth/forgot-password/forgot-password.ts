// forgot-password.ts
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { timeout } from 'rxjs';
import { AuthService } from '../../../services/auth-service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './forgot-password.html'
})
export class ForgotPassword {
  private authService = inject(AuthService);
  private router      = inject(Router);
  private cdr         = inject(ChangeDetectorRef);

  email    = '';
  loading  = false;
  error    = '';
  success  = false;

  submit() {
    if (!this.email) return;
    this.loading = true;
    this.error   = '';

    this.authService.forgotPassword(this.email)
      .pipe(timeout(15000))
      .subscribe({
      next: (res) => {
        this.loading = false;
        this.success = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        console.error('Forgot password error:', err);
        if (err.name === 'TimeoutError') {
          this.error = 'Request timed out. Please check if the backend is running.';
        } else if (err.status === 0) {
          this.error = 'Cannot reach server (Status 0). Please check your connection or backend port.';
        } else {
          this.error = `Error ${err.status}: ` + (err.error?.message || 'No account found with this email.');
        }
        this.cdr.detectChanges();
      }
    });
  }
}