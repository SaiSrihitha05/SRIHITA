import { CommonModule } from '@angular/common';
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-login',
  imports: [RouterModule, CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  loginForm: FormGroup;
  submitted = false;
  errorMessage = '';
  captchaCode = '';
  showPassword = false;

  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private fb = inject(FormBuilder);

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      captcha: ['', [Validators.required]]
    });
  }

  ngOnInit() {
    this.loadCaptcha(); // Load captcha on start
  }

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
  get captcha() { return this.loginForm.get('captcha'); }

  get isCaptchaVerified() {
    return this.captcha?.value === this.captchaCode && this.captchaCode !== '';
  }

  get isCaptchaIncorrect() {
    const val = this.captcha?.value;
    return val && val.length > 0 && val !== this.captchaCode;
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  loadCaptcha() {
    this.authService.getCaptcha().subscribe({
      next: (res) => {
        this.captchaCode = res.captchaCode;
        this.cdr.detectChanges();
        console.log("New Captcha:", this.captchaCode);
      },
      error: (err) => console.error("Captcha failed to load", err)
    });
  }

  onLogin() {
    this.submitted = true;
    this.errorMessage = '';

    if (this.loginForm.invalid) return;

    // Validation for captcha - Case Sensitive
    if (this.captcha?.value !== this.captchaCode) {
      this.errorMessage = 'Incorrect CAPTCHA. Please try again.';
      this.loadCaptcha();
      return;
    }

    const credentials = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password
    };
    //testing git
    //role based redirection
    this.authService.login(credentials).subscribe({
      next: (response) => {
        localStorage.setItem('token', response.token);
        localStorage.setItem('email', response.email);
        localStorage.setItem('role', response.role);
        if (response.role === 'Admin') {
          this.router.navigate(['/admin-dashboard']);
        } else if (response.role === 'Agent') {
          this.router.navigate(['/agent-dashboard']);
        } else if (response.role === 'ClaimsOfficer') {
          this.router.navigate(['/claims-officer-dashboard']);
        } else {
          this.router.navigate(['/customer-dashboard']);
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Invalid email or password';
        this.loadCaptcha();
      }
    });
  }
}