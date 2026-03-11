import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { BuyPolicy } from './buy-policy';
import { PlanService } from '../../../services/plan-service';
import { PolicyService } from '../../../services/policy-service';
import { of } from 'rxjs';

describe('BuyPolicy', () => {
  let component: BuyPolicy;
  let fixture: ComponentFixture<BuyPolicy>;
  let mockPlanService: any;
  let mockPolicyService: any;

  beforeEach(async () => {
    mockPlanService = jasmine.createSpyObj('PlanService', ['getPlanById']);
    mockPolicyService = jasmine.createSpyObj('PolicyService', ['getPolicyById', 'saveDraft', 'updateDraft']);

    await TestBed.configureTestingModule({
      imports: [BuyPolicy],
      providers: [
        { provide: PlanService, useValue: mockPlanService },
        { provide: PolicyService, useValue: mockPolicyService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BuyPolicy);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should identify Whole Life plans', () => {
    component.selectedPlan = { planType: 'WholeLife' };
    expect(component.isWholeLifePlan).toBeTrue();

    component.selectedPlan = { planType: 'TermLife' };
    expect(component.isWholeLifePlan).toBeFalse();
  });

  it('should validate future dates correctly', () => {
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(today.getDate() + 1);
    const yesterday = new Date(today);
    yesterday.setDate(today.getDate() - 1);

    const toISO = (d: Date) => d.toISOString().split('T')[0];

    component.policyData.startDate = toISO(today);
    expect(component.isFutureDate()).toBeTrue();

    component.policyData.startDate = toISO(tomorrow);
    expect(component.isFutureDate()).toBeTrue();

    component.policyData.startDate = toISO(yesterday);
    expect(component.isFutureDate()).toBeFalse();

    component.policyData.startDate = '';
    expect(component.isFutureDate()).toBeFalse();
  });

  it('should calculate age correctly', () => {
    const dob = '1990-01-01';
    // Assuming today's year is at least 2024
    const age = component.getAge(dob);
    expect(age).toBeGreaterThanOrEqual(34);
  });

  it('should calculate total premium with various factors', () => {
    component.selectedPlan = {
      baseRate: 5.0, // Per 1000
      minCoverageAmount: 100000,
      maxCoverageAmount: 1000000
    };
    component.policyData = {
      startDate: '2027-01-01',
      premiumFrequency: 'Yearly',
      termYears: 25
    };
    component.members = [{
      MemberName: 'Test',
      CoverageAmount: 100000, // (100000/1000) * 5 = 500
      DateOfBirth: '1995-01-01', // Age ~31, ageFactor = 1.0
      IsSmoker: false, // factor = 1.0
      Gender: 'Male', // factor = 1.0
    }];

    // Calculation: 500 * 1.0 (age) * 1.0 (smoker) * 1.0 (gender) * 1.2 (term) * 1.18 (GST)
    // 500 * 1.2 * 1.18 = 708
    const premium = component.calculateTotalPremium();
    expect(premium).toBe(708);
  });

  it('should validate form for Step 1', () => {
    component.selectedPlan = {
      minTermYears: 5,
      maxTermYears: 50,
      maxPolicyMembersAllowed: 5,
      minNominees: 1,
      maxNominees: 3
    };
    component.policyData = {
      startDate: '2027-01-01',
      termYears: 10,
      premiumFrequency: 'Monthly'
    };
    component.numMembers = 1;
    component.numNominees = 1;

    expect(component.isFormValid()).toBeTrue();

    component.policyData.termYears = 1; // Too low
    expect(component.isFormValid()).toBeFalse();
  });
});
