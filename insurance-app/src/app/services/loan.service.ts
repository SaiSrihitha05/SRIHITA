import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LoanResponseDto, ApplyLoanDto, RepayLoanDto } from '../models/loan.model';

@Injectable({
    providedIn: 'root'
})
export class LoanService {
    private http = inject(HttpClient);
    private baseUrl = 'https://localhost:7027/api/Loans';

    applyForLoan(dto: ApplyLoanDto): Observable<LoanResponseDto> {
        return this.http.post<LoanResponseDto>(`${this.baseUrl}/apply`, dto);
    }

    repayLoan(dto: RepayLoanDto): Observable<LoanResponseDto> {
        return this.http.post<LoanResponseDto>(`${this.baseUrl}/repay`, dto);
    }

    getMyLoans(): Observable<LoanResponseDto[]> {
        return this.http.get<LoanResponseDto[]>(`${this.baseUrl}/my-loans`);
    }

    getLoanById(id: number): Observable<LoanResponseDto> {
        return this.http.get<LoanResponseDto>(`${this.baseUrl}/${id}`);
    }

    getOutstandingLoan(policyId: number): Observable<{ outstandingBalance: number }> {
        return this.http.get<{ outstandingBalance: number }>(`${this.baseUrl}/outstanding/${policyId}`);
    }

    // Admin methods
    getAllLoans(): Observable<LoanResponseDto[]> {
        return this.http.get<LoanResponseDto[]>(`${this.baseUrl}/all`);
    }

    getLoansByPolicy(policyId: number): Observable<LoanResponseDto[]> {
        return this.http.get<LoanResponseDto[]>(`${this.baseUrl}/by-policy/${policyId}`);
    }
}
