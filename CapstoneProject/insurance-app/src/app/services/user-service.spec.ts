import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { UserService } from './user-service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch customers', () => {
    const mockUsers = [{ id: 1, name: 'John' }];

    service.getCustomers().subscribe(users => {
      expect(users).toEqual(mockUsers);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Users/customers');
    expect(req.request.method).toBe('GET');
    req.flush(mockUsers);
  });

  it('should update profile', () => {
    const mockProfile = { name: 'John Updated' };

    service.updateProfile(mockProfile).subscribe(res => {
      expect(res).toEqual(mockProfile);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Users/profile');
    expect(req.request.method).toBe('PUT');
    req.flush(mockProfile);
  });
});
