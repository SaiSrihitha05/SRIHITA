import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AdminPolicies } from './admin-policies';
import { PolicyService } from '../../../services/policy-service';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { of } from 'rxjs';

describe('AdminPolicies', () => {
  let component: AdminPolicies;
  let fixture: ComponentFixture<AdminPolicies>;
  let policyService: PolicyService;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPolicies],
      providers: [
        PolicyService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminPolicies);
    component = fixture.componentInstance;
    policyService = TestBed.inject(PolicyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should filter out draft policies on load', () => {
    const mockPolicies = [
      { id: 1, policyNumber: 'POL-001', status: 'Active' },
      { id: 2, policyNumber: 'POL-002', status: 'Draft' }
    ];
    spyOn(policyService, 'getAllPolicies').and.returnValue(of(mockPolicies));

    component.loadData();

    // Handle the agents fetch
    const req = httpMock.expectOne('https://localhost:7027/api/Users/agents');
    req.flush([]);

    expect(component.policies.length).toBe(1);
    expect(component.policies[0].status).toBe('Active');
  });

  it('should assign an agent and reload data', () => {
    spyOn(policyService, 'assignAgent').and.returnValue(of({}));
    spyOn(component, 'loadData');
    spyOn(window, 'alert');

    const event = { target: { value: '10' } };
    component.onAssignAgent(1, event);

    expect(policyService.assignAgent).toHaveBeenCalledWith(1, 10);
    expect(component.loadData).toHaveBeenCalled();
    expect(window.alert).toHaveBeenCalledWith('Agent assigned successfully!');
  });
});
