import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PolicyService } from './policy-service';

describe('PolicyService', () => {
  let service: PolicyService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PolicyService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(PolicyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch all policies (admin)', () => {
    const mockPolicies = [{ id: 1, policyNumber: 'POL-101' }];

    service.getAllPolicies().subscribe(policies => {
      expect(policies).toEqual(mockPolicies);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Policies');
    expect(req.request.method).toBe('GET');
    req.flush(mockPolicies);
  });

  it('should buy a policy (customer)', () => {
    const dummyFormData = new FormData();
    const mockResponse = { id: 1, status: 'Pending' };

    service.buyPolicy(dummyFormData).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Policies');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should download a document as Blob', () => {
    const mockBlob = new Blob(['content'], { type: 'application/pdf' });

    service.downloadFile(123).subscribe(res => {
      expect(res instanceof Blob).toBeTrue();
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Policies/download-document/123');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(mockBlob);
  });
});
