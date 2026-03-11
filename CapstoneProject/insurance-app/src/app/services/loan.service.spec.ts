import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { LoanService } from './loan.service';
import { LoanResponseDto, ApplyLoanDto, RepayLoanDto } from '../models/loan.model';

describe('LoanService', () => {
    let service: LoanService;
    let httpMock: HttpTestingController;

    const mockLoan: any = {
        id: 1,
        policyAssignmentId: 101,
        policyNumber: 'POL001',
        planName: 'Test Plan',
        customerName: 'John Doe',
        customerEmail: 'john@example.com',
        loanAmount: 5000,
        outstandingBalance: 5000,
        interestRate: 8,
        totalInterestPaid: 0,
        status: 'Active',
        loanDate: '2026-01-01T00:00:00',
        repayments: []
    };

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [LoanService]
        });
        service = TestBed.inject(LoanService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should apply for a loan', () => {
        const dto: ApplyLoanDto = { policyAssignmentId: 101 };
        service.applyForLoan(dto).subscribe(response => {
            expect(response).toEqual(mockLoan);
        });

        const req = httpMock.expectOne('https://localhost:7027/api/Loans/apply');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(dto);
        req.flush(mockLoan);
    });

    it('should repay a loan', () => {
        const dto: RepayLoanDto = { policyLoanId: 1, amount: 1000 };
        const updatedLoan = { ...mockLoan, outstandingBalance: 4000 };
        service.repayLoan(dto).subscribe(response => {
            expect(response.outstandingBalance).toBe(4000);
        });

        const req = httpMock.expectOne('https://localhost:7027/api/Loans/repay');
        expect(req.request.method).toBe('POST');
        req.flush(updatedLoan);
    });

    it('should get my loans', () => {
        const mockLoans = [mockLoan];
        service.getMyLoans().subscribe(loans => {
            expect(loans.length).toBe(1);
            expect(loans).toEqual(mockLoans);
        });

        const req = httpMock.expectOne('https://localhost:7027/api/Loans/my-loans');
        expect(req.request.method).toBe('GET');
        req.flush(mockLoans);
    });

    it('should get loan by id', () => {
        service.getLoanById(1).subscribe(loan => {
            expect(loan).toEqual(mockLoan);
        });

        const req = httpMock.expectOne('https://localhost:7027/api/Loans/1');
        expect(req.request.method).toBe('GET');
        req.flush(mockLoan);
    });

    it('should get outstanding balance', () => {
        const response = { outstandingBalance: 5000 };
        service.getOutstandingLoan(101).subscribe(res => {
            expect(res.outstandingBalance).toBe(5000);
        });

        const req = httpMock.expectOne('https://localhost:7027/api/Loans/outstanding/101');
        expect(req.request.method).toBe('GET');
        req.flush(response);
    });

    it('should get all loans (admin)', () => {
        const mockLoans = [mockLoan];
        service.getAllLoans().subscribe(loans => {
            expect(loans.length).toBe(1);
        });

        const req = httpMock.expectOne('https://localhost:7027/api/Loans/all');
        expect(req.request.method).toBe('GET');
        req.flush(mockLoans);
    });
});
