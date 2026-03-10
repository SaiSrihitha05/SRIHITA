// reset-password.ts
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth-service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reset-password.html'
})
export class ResetPassword implements OnInit {
  private route       = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private router      = inject(Router);

  token           = '';
  newPassword     = '';
  confirmPassword = '';
  showPassword    = false;
  loading         = false;
  error           = '';
  success         = false;

  ngOnInit() {
    // ✅ Read token from query param
    this.token = this.route.snapshot.queryParams['token'] || '';
    if (!this.token) {
      this.router.navigate(['/forgot-password']);
    }
  }

  submit() {
    this.error = '';

    if (this.newPassword.length < 6) {
      this.error = 'Password must be at least 6 characters.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }

    this.loading = true;

    this.authService.resetPassword(
      this.token, this.newPassword
    ).subscribe({
      next: () => {
        this.loading = false;
        this.success = true;
        // Redirect to login after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err) => {
        this.loading = false;
        this.error   = err.error?.message || 'Something went wrong.';
      }
    });
  }
}