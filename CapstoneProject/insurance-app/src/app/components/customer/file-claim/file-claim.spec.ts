import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FileClaim } from './file-claim';
import { ClaimService } from '../../../services/claim-service';
import { PolicyService } from '../../../services/policy-service';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';

describe('FileClaim', () => {
  let component: FileClaim;
  let fixture: ComponentFixture<FileClaim>;
  let policyService: PolicyService;
  let claimService: ClaimService;
  let router: Router;

  const mockRoute = {
    snapshot: { queryParams: { policyId: '101' } }
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FileClaim, FormsModule],
      providers: [
        PolicyService,
        ClaimService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: mockRoute },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FileClaim);
    component = fixture.componentInstance;
    policyService = TestBed.inject(PolicyService);
    claimService = TestBed.inject(ClaimService);
    router = TestBed.inject(Router);
  });

  it('should create and load policy details', () => {
    const mockPolicy = {
      id: 101,
      members: [{ id: 1, isPrimaryInsured: true, coverageAmount: 500000 }],
      bonusDetails: { totalBonus: 10000 },
      planHasBonus: true
    };
    spyOn(policyService, 'getPolicyById').and.returnValue(of(mockPolicy));

    component.ngOnInit();

    expect(policyService.getPolicyById).toHaveBeenCalledWith(101);
    expect(component.policy).toEqual(mockPolicy);
    expect(component.totalClaimAmount).toBe(510000);
  });

  it('should redirect if no policyId in route', () => {
    TestBed.inject(ActivatedRoute).snapshot.queryParams = {};
    spyOn(window, 'alert');

    component.ngOnInit();

    expect(window.alert).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/customer-dashboard/my-policies']);
  });

  it('should validate form based on claim type (Death requires cert)', () => {
    component.claimData.claimType = 'Death';
    component.claimData.policyMemberId = 1;
    component.claimData.deathCertificateNumber = '';

    expect(component.isClaimValid()).toBeFalse();

    component.claimData.deathCertificateNumber = 'DC123';
    component.selectedFiles = [new File([''], 'doc.pdf')];
    expect(component.isClaimValid()).toBeTrue();
  });

 
});
