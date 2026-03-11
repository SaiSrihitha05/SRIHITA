import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ClaimService } from './claim-service';

describe('ClaimService', () => {
  let service: ClaimService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ClaimService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(ClaimService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch all claims (admin)', () => {
    const mockClaims = [{ id: 1, policyNumber: 'POL-001' }];

    service.getAllClaims().subscribe(claims => {
      expect(claims.length).toBe(1);
      expect(claims).toEqual(mockClaims);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Claims');
    expect(req.request.method).toBe('GET');
    req.flush(mockClaims);
  });

  it('should file a claim (customer)', () => {
    const dummyFormData = new FormData();
    const mockResponse = { message: 'Claim filed' };

    service.fileClaim(dummyFormData).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Claims');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });
});
