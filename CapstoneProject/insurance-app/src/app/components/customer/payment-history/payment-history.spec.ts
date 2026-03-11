import { TestBed, ComponentFixture } from '@angular/core/testing';
import { PaymentHistory } from './payment-history';
import { PaymentService } from '../../../services/payment-service';
import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('PaymentHistory', () => {
  let component: PaymentHistory;
  let fixture: ComponentFixture<PaymentHistory>;
  let paymentService: PaymentService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentHistory],
      providers: [
        PaymentService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PaymentHistory);
    component = fixture.componentInstance;
    paymentService = TestBed.inject(PaymentService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });


  it('should download invoice', () => {
    const mockBlob = new Blob(['test'], { type: 'application/pdf' });
    spyOn(paymentService, 'downloadInvoice').and.returnValue(of(mockBlob));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:url');
    spyOn(window.URL, 'revokeObjectURL');

    component.download(1);

    expect(paymentService.downloadInvoice).toHaveBeenCalledWith(1);
    expect(window.URL.createObjectURL).toHaveBeenCalled();
  });
});
