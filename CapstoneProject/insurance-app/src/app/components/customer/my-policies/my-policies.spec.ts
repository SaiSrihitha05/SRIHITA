import { TestBed, ComponentFixture } from '@angular/core/testing';
import { MyPolicies } from './my-policies';
import { PolicyService } from '../../../services/policy-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';

describe('MyPolicies', () => {
  let component: MyPolicies;
  let fixture: ComponentFixture<MyPolicies>;
  let policyService: PolicyService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyPolicies],
      providers: [
        PolicyService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MyPolicies);
    component = fixture.componentInstance;
    policyService = TestBed.inject(PolicyService);
    router = TestBed.inject(Router);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load policies and unique statuses on init', () => {
    const mockPolicies = [
      { id: 1, status: 'Active' },
      { id: 2, status: 'Lapsed' }
    ];
    spyOn(policyService, 'getMyPolicies').and.returnValue(of(mockPolicies));

    component.ngOnInit();

    expect(component.policies.length).toBe(2);
    expect(component.uniqueStatuses).toEqual(['All', 'Active', 'Lapsed']);
  });

  it('should allow payment only for Active policies', () => {
    expect(component.canPayPremium({ status: 'Active' })).toBeTrue();
    expect(component.canPayPremium({ status: 'Lapsed' })).toBeFalse();
  });

  it('should navigate to payment on processPayment', () => {
    const spy = spyOn(router, 'navigate');
    const policy = { id: 1, totalPremiumAmount: 5000 };

    component.processPayment(policy);

    expect(spy).toHaveBeenCalledWith(['/customer-dashboard/pay-premium'], {
      queryParams: { policyId: 1, amount: 5000 }
    });
  });
});
