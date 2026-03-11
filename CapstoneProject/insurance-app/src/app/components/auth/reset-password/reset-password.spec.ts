import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { ResetPassword } from './reset-password';
import { AuthService } from '../../../services/auth-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

describe('ResetPassword', () => {
  let component: ResetPassword;
  let fixture: ComponentFixture<ResetPassword>;
  let authService: AuthService;
  let router: Router;

  let mockRoute: any;

  beforeEach(async () => {
    mockRoute = {
      snapshot: { queryParams: { token: 'valid-token' } }
    };

    await TestBed.configureTestingModule({
      imports: [ResetPassword, FormsModule],
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: mockRoute },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ResetPassword);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('should create and read token from route', () => {
    component.ngOnInit();
    expect(component.token).toBe('valid-token');
  });

  it('should redirect if no token present', () => {
    TestBed.inject(ActivatedRoute).snapshot.queryParams = {};
    component.ngOnInit();
    expect(router.navigate).toHaveBeenCalledWith(['/forgot-password']);
  });


  it('should show error message on failure', () => {
    spyOn(authService, 'resetPassword').and.returnValue(throwError(() => ({ error: { message: 'Expired' } })));

    component.token = 'invalid';
    component.newPassword = 'password123';
    component.confirmPassword = 'password123';
    component.submit();

    expect(component.error).toBe('Expired');
    expect(component.loading).toBeFalse();
  });
});
