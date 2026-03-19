import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AdminPlans } from './admin-plans';
import { PlanService } from '../../../services/plan-service';
import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('AdminPlans', () => {
  let component: AdminPlans;
  let fixture: ComponentFixture<AdminPlans>;
  let planService: PlanService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPlans],
      providers: [
        PlanService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminPlans);
    component = fixture.componentInstance;
    planService = TestBed.inject(PlanService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load all plans without filtering by status', () => {
    const mockPlans = [
      { id: 1, planName: 'Plan A', isActive: true },
      { id: 2, planName: 'Plan B', isActive: false }
    ];
    spyOn(planService, 'getPlans').and.returnValue(of(mockPlans));

    component.loadPlans();

    expect(component.plans.length).toBe(2);
  });

  it('should call createPlan when saving a new plan with valid data', () => {
    spyOn(planService, 'createPlan').and.returnValue(of({}));
    spyOn(component, 'loadPlans');

    component.isEditMode = false;
    component.currentPlan = {
      planName: 'New Plan', planType: 'Health', description: 'A valid description used in testing',
      baseRate: 5, commissionRate: 2, minAge: 18, maxAge: 60,
      minCoverageAmount: 1000, maxCoverageAmount: 50000,
      minTermYears: 1, maxTermYears: 10, gracePeriodDays: 30,
      maxPolicyMembersAllowed: 1, minNominees: 1, maxNominees: 5
    };
    component.savePlan();

    expect(planService.createPlan).toHaveBeenCalled();
    expect(component.loadPlans).toHaveBeenCalled();
    expect(component.showModal).toBeFalse();
  });

  it('should call updatePlan when saving an existing plan with valid data', () => {
    spyOn(planService, 'updatePlan').and.returnValue(of({}));
    spyOn(component, 'loadPlans');

    component.isEditMode = true;
    component.currentPlan = {
      id: 1, planName: 'Updated Plan', planType: 'Life', description: 'Another valid description for testing',
      baseRate: 4, commissionRate: 1, minAge: 20, maxAge: 65,
      minCoverageAmount: 2000, maxCoverageAmount: 60000,
      minTermYears: 2, maxTermYears: 20, gracePeriodDays: 30,
      maxPolicyMembersAllowed: 2, minNominees: 1, maxNominees: 5
    };
    component.savePlan();

    expect(planService.updatePlan).toHaveBeenCalledWith(1, jasmine.any(Object));
    expect(component.loadPlans).toHaveBeenCalled();
    expect(component.showModal).toBeFalse();
  });
});
