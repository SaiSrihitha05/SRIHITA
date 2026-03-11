import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminLoans } from './admin-loans';
import { LoanService } from '../../../services/loan.service';
import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('AdminLoans', () => {
    let component: AdminLoans;
    let fixture: ComponentFixture<AdminLoans>;
    let mockLoanService: any;

    const mockLoans = [
        { id: 1, loanAmount: 5000, outstandingBalance: 5000, status: 'Active' },
        { id: 2, loanAmount: 10000, outstandingBalance: 0, status: 'Closed' },
        { id: 3, loanAmount: 2000, outstandingBalance: 2000, status: 'Active' }
    ];

    beforeEach(async () => {
        mockLoanService = jasmine.createSpyObj('LoanService', ['getAllLoans']);
        mockLoanService.getAllLoans.and.returnValue(of(mockLoans));

        await TestBed.configureTestingModule({
            imports: [AdminLoans],
            providers: [
                { provide: LoanService, useValue: mockLoanService },
                provideHttpClient(),
                provideHttpClientTesting()
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(AdminLoans);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should fetch all loans on init', () => {
        expect(mockLoanService.getAllLoans).toHaveBeenCalled();
        expect(component.loans.length).toBe(3);
    });

    it('should filter loans correctly', () => {
        component.setFilter('Active');
        expect(component.filtered.length).toBe(2);
        expect(component.filtered.every(l => l.status === 'Active')).toBeTrue();

        component.setFilter('Closed');
        expect(component.filtered.length).toBe(1);
        expect(component.filtered[0].id).toBe(2);

        component.setFilter('All');
        expect(component.filtered.length).toBe(3);
    });

    it('should calculate total active loans', () => {
        expect(component.totalActive).toBe(2);
    });

    it('should calculate total outstanding balance', () => {
        expect(component.totalOutstanding).toBe(7000); // 5000 + 2000
    });

    it('should calculate total disbursed amount', () => {
        expect(component.totalDisbursed).toBe(17000); // 5000 + 10000 + 2000
    });

    it('should return correct status classes', () => {
        expect(component.getStatusClass('Active')).toContain('bg-blue-100');
        expect(component.getStatusClass('Closed')).toContain('bg-green-100');
        expect(component.getStatusClass('Adjusted')).toContain('bg-purple-100');
        expect(component.getStatusClass('Unknown')).toContain('bg-gray-100');
    });
});
