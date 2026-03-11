import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { PayPremium } from './pay-premium';
import { PolicyService } from '../../../services/policy-service';
import { of } from 'rxjs';

describe('PayPremium', () => {
  let component: PayPremium;
  let fixture: ComponentFixture<PayPremium>;
  let mockPolicyService: any;

  beforeEach(async () => {
    mockPolicyService = jasmine.createSpyObj('PolicyService', ['getPolicyById']);

    await TestBed.configureTestingModule({
      imports: [PayPremium],
      providers: [
        { provide: PolicyService, useValue: mockPolicyService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PayPremium);
    component = fixture.componentInstance;
    // Do not detectChanges yet, we want to set up the policy mock
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should determine correct installment labels', () => {
    component.policy = { premiumFrequency: 'Monthly' };
    expect(component.installmentLabel).toBe('Month');

    component.policy = { premiumFrequency: 'Quarterly' };
    expect(component.installmentLabel).toBe('Quarter');

    component.policy = { premiumFrequency: 'Yearly' };
    expect(component.installmentLabel).toBe('Year');
  });

  it('should provide correct installment options', () => {
    component.policy = { premiumFrequency: 'Monthly' };
    const monthlyOptions = component.installmentOptions;
    expect(monthlyOptions.length).toBe(4); // 0, 1, 2, 5
    expect(monthlyOptions[1].label).toBe('Next 1 Month');

    component.policy = { premiumFrequency: 'Quarterly' };
    const quarterlyOptions = component.installmentOptions;
    expect(quarterlyOptions.length).toBe(4); // 0, 1, 2, 3
    expect(quarterlyOptions[3].label).toBe('Next 3 Quarters');

    component.policy = { premiumFrequency: 'Yearly' };
    const yearlyOptions = component.installmentOptions;
    expect(yearlyOptions.length).toBe(3); // 0, 1, 2
  });

  it('should calculate total amount correctly', () => {
    component.policy = { totalPremiumAmount: 1200 };
    component.paymentData.extraInstallments = 0;
    expect(component.totalAmount).toBe(1200);

    component.paymentData.extraInstallments = 2;
    expect(component.totalAmount).toBe(3600); // 1200 * (1 + 2)
  });
});
