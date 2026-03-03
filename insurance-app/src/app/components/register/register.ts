import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-register',
  imports: [RouterModule, CommonModule, ReactiveFormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  registerForm: FormGroup;
  submitted = false;
  errorMessage = '';

  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private fb = inject(FormBuilder);

  constructor() {
    this.registerForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9\-\+\(\)\s]+$/)]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  get name() {
    return this.registerForm.get('name');
  }

  get email() {
    return this.registerForm.get('email');
  }

  get phone() {
    return this.registerForm.get('phone');
  }

  get password() {
    return this.registerForm.get('password');
  }

  onRegister() {
    this.submitted = true;
    this.errorMessage = '';

    if (this.registerForm.invalid) {
      return;
    }

    const userData = this.registerForm.value;

    this.authService.register(userData).subscribe({
      next: () => {
        alert('Registration successful! Please login.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Registration failed';
        console.error('Registration failed', err);
        this.cdr.detectChanges();
      }
    });
  }
}
