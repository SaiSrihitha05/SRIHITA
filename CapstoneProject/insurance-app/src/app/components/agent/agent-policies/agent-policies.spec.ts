import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AgentPolicies } from './agent-policies';
import { PolicyService } from '../../../services/policy-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';

describe('AgentPolicies', () => {
  let component: AgentPolicies;
  let fixture: ComponentFixture<AgentPolicies>;
  let policyService: PolicyService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AgentPolicies, FormsModule],
      providers: [
        PolicyService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AgentPolicies);
    component = fixture.componentInstance;
    policyService = TestBed.inject(PolicyService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load agent policies on init', () => {
    const mockPolicies = [{ id: 1, policyNumber: 'POL-001' }];
    spyOn(policyService, 'getAgentPolicies').and.returnValue(of(mockPolicies));

    component.ngOnInit();

    expect(policyService.getAgentPolicies).toHaveBeenCalled();
    expect(component.policies).toEqual(mockPolicies);
    expect(component.loading).toBeFalse();
  });


  it('should show alert on status update failure', () => {
    const mockPolicy = { id: 1, status: 'Active' };
    spyOn(window, 'prompt').and.returnValue('Verified');
    spyOn(policyService, 'updatePolicyStatus').and.returnValue(throwError(() => new Error('Error')));
    spyOn(window, 'alert');
    spyOn(component, 'loadMyPolicies');

    component.onUpdateStatus(mockPolicy);

    expect(window.alert).toHaveBeenCalledWith('Status update failed');
    expect(component.loadMyPolicies).toHaveBeenCalled();
  });
});
