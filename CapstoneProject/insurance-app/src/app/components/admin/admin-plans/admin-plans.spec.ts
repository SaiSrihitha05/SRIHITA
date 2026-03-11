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

  it('should filter out draft plans on load', () => {
    const mockPlans = [
      { id: 1, planName: 'Plan A', status: 'Active' },
      { id: 2, planName: 'Plan B', status: 'Draft' }
    ];
    spyOn(planService, 'getPlans').and.returnValue(of(mockPlans));

    component.loadPlans();

    expect(component.plans.length).toBe(1);
    expect(component.plans[0].id).toBe(1);
  });

  it('should call createPlan when saving a new plan', () => {
    spyOn(planService, 'createPlan').and.returnValue(of({}));
    spyOn(component, 'loadPlans');

    component.isEditMode = false;
    component.currentPlan = { planName: 'New Plan' };
    component.savePlan();

    expect(planService.createPlan).toHaveBeenCalledWith(component.currentPlan);
    expect(component.loadPlans).toHaveBeenCalled();
    expect(component.showModal).toBeFalse();
  });

  it('should call updatePlan when saving an existing plan', () => {
    spyOn(planService, 'updatePlan').and.returnValue(of({}));
    spyOn(component, 'loadPlans');

    component.isEditMode = true;
    component.currentPlan = { id: 1, planName: 'Updated Plan' };
    component.savePlan();

    expect(planService.updatePlan).toHaveBeenCalledWith(1, component.currentPlan);
    expect(component.loadPlans).toHaveBeenCalled();
    expect(component.showModal).toBeFalse();
  });
});
