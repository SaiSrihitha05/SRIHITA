import { TestBed, ComponentFixture } from '@angular/core/testing';
import { PolicyDetails } from './policy-details';
import { PolicyService } from '../../../services/policy-service';
import { LoanService } from '../../../services/loan.service';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('PolicyDetails', () => {
  let component: PolicyDetails;
  let fixture: ComponentFixture<PolicyDetails>;
  let policyService: PolicyService;
  let loanService: LoanService;
  let router: Router;

  const mockRoute = {
    snapshot: { paramMap: { get: () => '1' } }
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PolicyDetails],
      providers: [
        PolicyService,
        LoanService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: mockRoute },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PolicyDetails);
    component = fixture.componentInstance;
    policyService = TestBed.inject(PolicyService);
    loanService = TestBed.inject(LoanService);
    router = TestBed.inject(Router);
  });

  it('should create and load policy details', () => {
    const mockPolicy = { id: 1, status: 'Active', planHasLoanFacility: true };
    spyOn(policyService, 'getPolicyById').and.returnValue(of(mockPolicy));
    spyOn(component, 'fetchOutstandingLoan');

    component.ngOnInit();

    expect(policyService.getPolicyById).toHaveBeenCalledWith(1);
    expect(component.policy).toEqual(mockPolicy);
    expect(component.fetchOutstandingLoan).toHaveBeenCalled();
  });




  it('should apply for loan and refresh balance', () => {
    component.policy = { id: 1 };
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(loanService, 'applyForLoan').and.returnValue(of({ loanAmount: 5000 } as any));
    spyOn(window, 'alert');
    spyOn(component, 'fetchOutstandingLoan');

    component.onApplyLoan();

    expect(loanService.applyForLoan).toHaveBeenCalled();
    expect(window.alert).toHaveBeenCalledWith('Loan of ₹5000 approved successfully!');
    expect(component.fetchOutstandingLoan).toHaveBeenCalled();
  });
});
