import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth-service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call login and return data', () => {
    const mockResponse = { token: '123', role: 'Admin' };
    const credentials = { email: 'test@test.com', password: 'password' };

    service.login(credentials).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should handle logout correctly', () => {
    localStorage.setItem('token', 'some-token');
    localStorage.setItem('role', 'Admin');

    service.logout();

    expect(localStorage.getItem('token')).toBeNull();
    expect(localStorage.getItem('role')).toBeNull();
    expect(service.isLoggedIn()).toBeFalse();
  });
});
