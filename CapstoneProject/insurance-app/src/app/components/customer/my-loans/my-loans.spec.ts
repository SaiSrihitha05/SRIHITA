import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyLoans } from './my-loans';
import { LoanService } from '../../../services/loan.service';
import { PolicyService } from '../../../services/policy-service';
import { of } from 'rxjs';
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
        { id: 1, policyAssignmentId: 101, planName: 'Plan A', policyNumber: 'POL001', loanAmount: 5000, outstandingBalance: 5000, status: 'Active', loanDate: '2026-01-01' },
        { id: 2, policyAssignmentId: 102, planName: 'Plan B', policyNumber: 'POL002', loanAmount: 10000, outstandingBalance: 0, status: 'Closed', loanDate: '2025-01-01' }
    ];

    beforeEach(async () => {
        mockLoanService = jasmine.createSpyObj('LoanService', ['getMyLoans']);
        mockPolicyService = jasmine.createSpyObj('PolicyService', ['getMyPolicies']);

        mockLoanService.getMyLoans.and.returnValue(of(mockLoans));

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

    it('should load loans on init', () => {
        expect(mockLoanService.getMyLoans).toHaveBeenCalled();
        expect(component.loans.length).toBe(2);
        expect(component.loading).toBeFalse();
    });

    it('should return correct status classes', () => {
        expect(component.getStatusClass('Active')).toBe('bg-blue-100 text-blue-700');
        expect(component.getStatusClass('Closed')).toBe('bg-green-100 text-green-700');
        expect(component.getStatusClass('Adjusted')).toBe('bg-purple-100 text-purple-700');
        expect(component.getStatusClass('Unknown')).toBe('bg-gray-100 text-gray-700');
    });
});
