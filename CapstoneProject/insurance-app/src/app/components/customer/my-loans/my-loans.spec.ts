import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyLoans } from './my-loans';
import { LoanService } from '../../../services/loan.service';
import { PolicyService } from '../../../services/policy-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ChangeDetectorRef } from '@angular/core';

describe('MyLoans', () => {
    let component: MyLoans;
    let fixture: ComponentFixture<MyLoans>;
    let mockLoanService: any;
    let mockPolicyService: any;

    const mockLoans: any = [
        { id: 1, policyAssignmentId: 101, loanAmount: 5000, outstandingBalance: 5000, status: 'Active', repayments: [] },
        { id: 2, policyAssignmentId: 102, loanAmount: 10000, outstandingBalance: 0, status: 'Closed', repayments: [] }
    ];

    const mockPolicies = [
        {
            id: 101,
            status: 'Active',
            planHasLoanFacility: true,
            planLoanEligibleAfterYears: 1,
            startDate: '2024-01-01' // 2 years old
        },
        {
            id: 102,
            status: 'Active',
            planHasLoanFacility: true,
            planLoanEligibleAfterYears: 1,
            startDate: '2024-01-01'
        },
        {
            id: 103,
            status: 'Active',
            planHasLoanFacility: false, // No loan facility
            planLoanEligibleAfterYears: 1,
            startDate: '2024-01-01'
        },
        {
            id: 104,
            status: 'Active',
            planHasLoanFacility: true,
            planLoanEligibleAfterYears: 5, // Not yet eligible
            startDate: '2025-01-01'
        }
    ];

    beforeEach(async () => {
        mockLoanService = jasmine.createSpyObj('LoanService', ['getMyLoans', 'repayLoan']);
        mockPolicyService = jasmine.createSpyObj('PolicyService', ['getMyPolicies']);

        mockLoanService.getMyLoans.and.returnValue(of(mockLoans));
        mockPolicyService.getMyPolicies.and.returnValue(of(mockPolicies));

        await TestBed.configureTestingModule({
            imports: [MyLoans],
            providers: [
                { provide: LoanService, useValue: mockLoanService },
                { provide: PolicyService, useValue: mockPolicyService },
                provideHttpClient(),
                provideHttpClientTesting(),
                provideRouter([]),
                ChangeDetectorRef
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(MyLoans);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load loans and eligible policies on init', () => {
        expect(mockLoanService.getMyLoans).toHaveBeenCalled();
        expect(mockPolicyService.getMyPolicies).toHaveBeenCalled();
        expect(component.loans.length).toBe(2);
        expect(component.policies.length).toBe(4);
    });

    it('should correctly determine loan eligibility', () => {
        // Policy 101 has active loan
        const res1 = component.isEligible(mockPolicies[0]);
        expect(res1.eligible).toBeFalse();
        expect(res1.reason).toContain('Active loan already exists');

        // Policy 103 no facility
        const res3 = component.isEligible(mockPolicies[2]);
        expect(res3.eligible).toBeFalse();
        expect(res3.reason).toContain('Plan does not support loans');

        // Policy 104 not yet old enough
        const res4 = component.isEligible(mockPolicies[3]);
        expect(res4.eligible).toBeFalse();
        expect(res4.reason).toContain('Eligible after 5 years');

        // Policy 102 is eligible (loan is closed)
        const res2 = component.isEligible(mockPolicies[1]);
        expect(res2.eligible).toBeTrue();
    });

    it('should call repayLoan and refresh on success', () => {
        spyOn(window, 'prompt').and.returnValue('1000');
        spyOn(window, 'alert');
        mockLoanService.repayLoan.and.returnValue(of({}));
        const fetchSpy = spyOn(component, 'fetchLoans');

        component.repayLoan(1, 5000);

        expect(mockLoanService.repayLoan).toHaveBeenCalledWith({ policyLoanId: 1, amount: 1000 });
        expect(window.alert).toHaveBeenCalledWith('Repayment successful!');
        expect(fetchSpy).toHaveBeenCalled();
    });

    it('should show error alert if repayLoan fails', () => {
        spyOn(window, 'prompt').and.returnValue('1000');
        spyOn(window, 'alert');
        mockLoanService.repayLoan.and.returnValue(throwError(() => ({ error: { message: 'Failure' } })));

        component.repayLoan(1, 5000);

        expect(window.alert).toHaveBeenCalledWith('Failure');
    });
});
