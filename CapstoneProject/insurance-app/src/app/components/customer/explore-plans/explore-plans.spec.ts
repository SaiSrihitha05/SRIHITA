import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ExplorePlans } from './explore-plans';
import { PlanService } from '../../../services/plan-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { provideRouter } from '@angular/router';

describe('ExplorePlans', () => {
  let component: ExplorePlans;
  let fixture: ComponentFixture<ExplorePlans>;
  let planService: PlanService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExplorePlans, FormsModule],
      providers: [
        PlanService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ExplorePlans);
    component = fixture.componentInstance;
    planService = TestBed.inject(PlanService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load all plans on init', () => {
    const mockPlans = [{ id: 1, planName: 'Test Plan', planType: 'TermLife' }];
    spyOn(planService, 'getPlans').and.returnValue(of(mockPlans));

    component.ngOnInit();

    expect(planService.getPlans).toHaveBeenCalled();
    expect(component.plans).toEqual(mockPlans);
  });

  it('should apply filters and call getFilteredPlans', () => {
    const mockFilteredPlans = [{ id: 2, planName: 'Filtered Plan', planType: 'Savings' }];
    spyOn(planService, 'getFilteredPlans').and.returnValue(of(mockFilteredPlans));

    component.filters.planType = 'Savings';
    component.applyFilter();

    expect(planService.getFilteredPlans).toHaveBeenCalledWith(component.filters);
    expect(component.plans).toEqual(mockFilteredPlans);
  });

  it('should group plans correctly for UI', () => {
    component.plans = [
      { id: 1, planType: 'TermLife', planName: 'T1' },
      { id: 2, planType: 'Savings', planName: 'S1' }
    ];

    const groups = component.groupedPlans;
    expect(groups.length).toBe(2);
    expect(groups[0].key).toBe('TermLife');
    expect(groups[1].key).toBe('Savings');
  });
});
