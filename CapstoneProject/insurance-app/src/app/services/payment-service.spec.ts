import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PaymentService } from './payment-service';

describe('PaymentService', () => {
  let service: PaymentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PaymentService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(PaymentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should make a payment', () => {
    const mockDto = { policyId: 1, amount: 1000 };
    const mockResponse = { message: 'Success' };

    service.makePayment(mockDto).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Payments');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should download invoice as Blob', () => {
    const mockBlob = new Blob(['test data'], { type: 'application/pdf' });

    service.downloadInvoice(1).subscribe(res => {
      expect(res instanceof Blob).toBeTrue();
      expect(res.size).toBeGreaterThan(0);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Payments/1/invoice');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(mockBlob);
  });
});
