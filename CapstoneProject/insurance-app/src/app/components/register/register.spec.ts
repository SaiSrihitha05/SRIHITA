import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Register } from './register';
import { AuthService } from '../../services/auth-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Register, ReactiveFormsModule],
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('should create and initialize form', () => {
    expect(component).toBeTruthy();
    expect(component.registerForm).toBeDefined();
    expect(component.registerForm.get('email')).toBeDefined();
  });

  it('should show validation errors if form is invalid on submit', () => {
    component.onRegister();
    expect(component.submitted).toBeTrue();
    expect(component.registerForm.invalid).toBeTrue();
  });


  it('should handle conflict error (409)', () => {
    spyOn(authService, 'register').and.returnValue(throwError(() => ({ status: 409 })));

    component.registerForm.setValue({
      name: 'Test User',
      email: 'duplicate@example.com',
      phone: '1234567890',
      password: 'password123'
    });

    component.onRegister();

    expect(component.errorMessage).toBe('This email is already registered. Please use a different email or login.');
  });


});
