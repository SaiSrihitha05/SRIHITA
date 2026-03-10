// forgot-password.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
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

  email    = '';
  loading  = false;
  error    = '';

  submit() {
    if (!this.email) return;
    this.loading = true;
    this.error   = '';

    this.authService.forgotPassword(this.email).subscribe({
      next: (res) => {
        this.loading = false;
        // ✅ Navigate to reset page with token in query param
        this.router.navigate(['/reset-password'], {
          queryParams: { token: res.token }
        });
      },
      error: (err) => {
        this.loading = false;
        this.error   = err.error?.message || 'No account found with this email.';
      }
    });
  }
}