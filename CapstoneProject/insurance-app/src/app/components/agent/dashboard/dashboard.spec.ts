import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AgentDashboard } from './dashboard';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { CommonModule } from '@angular/common';

describe('Agent Dashboard', () => {
  let component: AgentDashboard;
  let fixture: ComponentFixture<AgentDashboard>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AgentDashboard],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideCharts(withDefaultRegisterables()),
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    }).overrideComponent(AgentDashboard, {
      set: { imports: [CommonModule, BaseChartDirective] }
    }).compileComponents();

    fixture = TestBed.createComponent(AgentDashboard);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });


  it('should fetch and process agent stats', () => {
    localStorage.setItem('token', 'fake-token');
    const mockData = {
      totalPoliciesSold: 5,
      totalCommissionEarned: 5000,
      totalAssignedPolicies: 5,
      assignedCustomers: 10,
      activePolicies: 3,
      pendingPolicies: 2,
      monthlyCommission: [
        { monthName: 'January', commissionEarned: 1000, policiesSold: 2 }
      ],
      monthlyPoliciesSold: [
        { monthName: 'January', policiesSold: 2 }
      ],
      recentAssignedPolicies: [
        { policyNumber: 'P001', customerName: 'Customer A', status: 'Active' }
      ],
      policiesByPlanType: [
        { planType: 'Life', count: 5 }
      ]
    };

    fixture.detectChanges(); // Trigger ngOnInit which calls fetchAgentStats

    const req = httpMock.expectOne('https://localhost:7027/api/Dashboard/agent');
    expect(req.request.method).toBe('GET');
    req.flush(mockData);

    expect(component.stats).toEqual(mockData);
    expect(component.loading).toBeFalse();

    // Verify chart data
    expect(component.earningsTrendData.labels).toContain('January');
    expect(component.planMixData.labels).toContain('Life');
    expect(component.healthData.datasets[0].data).toEqual([3, 2]);
  });

  it('should handle error during stats fetch', () => {
    localStorage.setItem('token', 'fake-token');
    spyOn(console, 'error');

    fixture.detectChanges(); // Trigger ngOnInit

    const req = httpMock.expectOne('https://localhost:7027/api/Dashboard/agent');
    req.error(new ProgressEvent('API Error'));

    expect(component.loading).toBeFalse();
    expect(console.error).toHaveBeenCalled();
  });
});