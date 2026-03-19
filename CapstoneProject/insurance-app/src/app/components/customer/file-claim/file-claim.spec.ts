import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FileClaim } from './file-claim';
import { ClaimService } from '../../../services/claim-service';
import { PolicyService } from '../../../services/policy-service';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { ChangeDetectorRef } from '@angular/core';

describe('FileClaim', () => {
    let component: FileClaim;
    let fixture: ComponentFixture<FileClaim>;
    let mockClaimService: any;
    let mockPolicyService: any;
    let mockRouter: any;

    const mockPolicies: any[] = [
        { id: 101, status: 'Active', planName: 'Plan A', policyNumber: 'POL101', members: [{ id: 1, name: 'User 1', status: 'Active', coverageAmount: 500000 }] },
        { id: 102, status: 'Active', planName: 'Plan B', policyNumber: 'POL102', members: [{ id: 2, name: 'User 2', status: 'Active', coverageAmount: 1000000 }] }
    ];

    const mockClaims: any[] = [];

    beforeEach(async () => {
        mockClaimService = jasmine.createSpyObj('ClaimService', ['getMyClaims', 'fileClaim']);
        mockPolicyService = jasmine.createSpyObj('PolicyService', ['getMyPolicies', 'getPolicyById']);
        mockRouter = jasmine.createSpyObj('Router', ['navigate']);

        mockPolicyService.getMyPolicies.and.returnValue(of(mockPolicies));
        mockClaimService.getMyClaims.and.returnValue(of(mockClaims));

        await TestBed.configureTestingModule({
            imports: [FileClaim, FormsModule],
            providers: [
                { provide: ClaimService, useValue: mockClaimService },
                { provide: PolicyService, useValue: mockPolicyService },
                { provide: Router, useValue: mockRouter },
                {
                    provide: ActivatedRoute,
                    useValue: {
                        snapshot: { queryParams: {} }
                    }
                },
                provideHttpClient(),
                provideHttpClientTesting(),
                ChangeDetectorRef
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(FileClaim);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should load policies on init', () => {
        fixture.detectChanges();
        expect(mockPolicyService.getMyPolicies).toHaveBeenCalled();
        expect(component.allPolicies.length).toBe(2);
    });

    it('should auto-select policy if only one is eligible', () => {
        // Mock only one eligible policy
        const singlePolicy = [mockPolicies[0]];
        mockPolicyService.getMyPolicies.and.returnValue(of(singlePolicy));
        mockPolicyService.getPolicyById.and.returnValue(of(singlePolicy[0]));
        
        fixture.detectChanges();
        
        expect(component.selectedPolicyId).toBe(101);
        expect(mockPolicyService.getPolicyById).toHaveBeenCalledWith(101);
    });

    it('should validate Death claim requiring certificate', () => {
        fixture.detectChanges();
        component.claimData.claimForMemberId = 1;
        component.claimData.claimType = 'Death';
        component.claimData.deathCertificateNumber = '';
        
        expect(component.isClaimValid()).toBeFalse();

        component.claimData.deathCertificateNumber = 'DC123';
        component.selectedFiles = [new File([''], 'test.pdf')];
        expect(component.isClaimValid()).toBeTrue();
    });
});
