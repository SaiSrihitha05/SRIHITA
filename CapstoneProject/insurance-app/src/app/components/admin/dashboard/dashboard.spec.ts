import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AdminDashboard } from './dashboard';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { BaseChartDirective } from 'ng2-charts';
import { CommonModule } from '@angular/common';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

describe('AdminDashboard', () => {
  let component: AdminDashboard;
  let fixture: ComponentFixture<AdminDashboard>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDashboard],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideCharts(withDefaultRegisterables())
      ]
    }).overrideComponent(AdminDashboard, {
      set: { imports: [CommonModule, BaseChartDirective] }
    }).compileComponents();

    fixture = TestBed.createComponent(AdminDashboard);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should fetch and process dashboard data on init', () => {
    const mockData = {
      totalPolicies: 10,
      activePolicies: 8,
      settledClaims: 5,
      totalClaims: 7,
      totalPremiumCollected: 100000,
      totalPaymentsCount: 50,
      claimApprovalRate: 71,
      totalAgents: 5,
      totalClaimsOfficers: 3,
      recentPolicies: [
        { id: 1, status: 'Active' },
        { id: 2, status: 'Draft' }
      ],
      revenueByPlanType: [
        { planName: 'Plan A', totalRevenue: 50000 }
      ],
      agentPerformance: [
        { agentName: 'Agent X', policiesSold: 5 }
      ],
      recentClaims: [
        { claimAmount: 10000 },
        { claimAmount: 20000 }
      ]
    };

    fixture.detectChanges(); // Trigger ngOnInit

    const req = httpMock.expectOne('https://localhost:7027/api/Dashboard/admin');
    expect(req.request.method).toBe('GET');
    req.flush(mockData);

    expect(component.stats).toBeTruthy();
    expect(component.stats.recentPolicies.length).toBe(1); // Draft filtered out
    expect(component.loading).toBeFalse();

    // Verify chart data preparation
    expect(component.pieChartData.labels).toContain('Plan A');
    expect(component.barChartData.labels).toContain('Agent X');
    expect(component.claimsEfficiencyData.datasets[0].data).toEqual([5, 2]); // Settled vs Pending
  });

  it('should handle error during data fetch', () => {
    spyOn(console, 'error');
    fixture.detectChanges(); // Trigger ngOnInit
    const req = httpMock.expectOne('https://localhost:7027/api/Dashboard/admin');
    req.error(new ProgressEvent('Network error'));

    expect(component.loading).toBeFalse();
    expect(console.error).toHaveBeenCalled();
  });
});
