import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ForgotPassword } from './forgot-password';
import { AuthService } from '../../../services/auth-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

describe('ForgotPassword', () => {
  let component: ForgotPassword;
  let fixture: ComponentFixture<ForgotPassword>;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ForgotPassword, FormsModule],
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPassword);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });


  it('should show error message on failure', () => {
    spyOn(authService, 'forgotPassword').and.returnValue(throwError(() => ({ error: { message: 'Not Found' } })));

    component.email = 'wrong@example.com';
    component.submit();

    expect(component.error).toBe('Not Found');
    expect(component.loading).toBeFalse();
  });

  it('should not submit if email is empty', () => {
    spyOn(authService, 'forgotPassword');
    component.email = '';
    component.submit();
    expect(authService.forgotPassword).not.toHaveBeenCalled();
  });
});
