import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { Login } from './login';
import { AuthService } from '../../services/auth-service';
import { of, throwError } from 'rxjs';
import { ReactiveFormsModule } from '@angular/forms';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let mockAuthService: any;
  let router: Router;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['login', 'getCaptcha']);
    mockAuthService.getCaptcha.and.returnValue(of({ captchaCode: 'ABCD' }));

    await TestBed.configureTestingModule({
      imports: [Login, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should validate email format', () => {
    const email = component.loginForm.controls['email'];
    email.setValue('invalid-email');
    expect(email.invalid).toBeTrue();

    email.setValue('test@example.com');
    expect(email.valid).toBeTrue();
  });

  it('should require captcha', () => {
    const captcha = component.loginForm.controls['captcha'];
    expect(captcha.valid).toBeFalse();
    captcha.setValue('ABCD');
    expect(captcha.valid).toBeTrue();
  });

  it('should show error for incorrect captcha', () => {
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123',
      captcha: 'WRONG'
    });
    component.onLogin();
    expect(component.errorMessage).toBe('Incorrect CAPTCHA. Please try again.');
  });

  it('should navigate to Admin dashboard on Admin login success', () => {
    const navSpy = spyOn(router, 'navigate');
    component.loginForm.patchValue({
      email: 'admin@test.com',
      password: 'password123',
      captcha: 'ABCD'
    });
    mockAuthService.login.and.returnValue(of({
      token: 'fake-token',
      email: 'admin@test.com',
      role: 'Admin'
    }));

    component.onLogin();
    fixture.detectChanges();

    expect(navSpy).toHaveBeenCalledWith(['/admin-dashboard']);
    expect(localStorage.getItem('role')).toBe('Admin');
  });

  it('should navigate to Customer dashboard on Customer login success', () => {
    const navSpy = spyOn(router, 'navigate');
    component.loginForm.patchValue({
      email: 'user@test.com',
      password: 'password123',
      captcha: 'ABCD'
    });
    mockAuthService.login.and.returnValue(of({
      token: 'fake-token',
      email: 'user@test.com',
      role: 'Customer'
    }));

    component.onLogin();
    fixture.detectChanges();

    expect(navSpy).toHaveBeenCalledWith(['/customer-dashboard']);
  });

  it('should show error message on login failure', () => {
    component.loginForm.patchValue({
      email: 'test@test.com',
      password: 'wrongpassword',
      captcha: 'ABCD'
    });
    mockAuthService.login.and.returnValue(throwError(() => ({
      error: { message: 'Invalid credentials' }
    })));

    component.onLogin();
    fixture.detectChanges();

    expect(component.errorMessage).toBe('Invalid credentials');
    expect(mockAuthService.getCaptcha).toHaveBeenCalled(); // Should reload captcha
  });
});
