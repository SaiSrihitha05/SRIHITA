import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoanService } from '../../../services/loan.service';
import { PolicyService } from '../../../services/policy-service'; // ✅ ADDED
import { LoanResponseDto } from '../../../models/loan.model';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';


@Component({
    selector: 'app-my-loans',
    standalone: true,
    imports: [CommonModule, RouterModule, FormsModule],
    templateUrl: './my-loans.html'
})
export class MyLoans implements OnInit {
    private loanService = inject(LoanService);
    private policyService = inject(PolicyService);
    private cdr = inject(ChangeDetectorRef);

    loans: LoanResponseDto[] = [];
    allPolicies: any[] = [];
    selectedPolicyId: number | null = null;
    eligibilityResult: { eligible: boolean; reason?: string; estimatedAmount?: number; interestRate?: number } | null = null;
    loading = true;
    isApplying = false;

    ngOnInit() {
        this.fetchLoans();
    }

    fetchLoans() {
        this.loading = true;
        this.loanService.getMyLoans().subscribe({
            next: (data) => {
                this.loans = data;
                this.loading = false;
                this.cdr.detectChanges();
            },
            error: (err) => {
                console.error('Error fetching loans', err);
                this.loading = false;
            }
        });
    }

    getStatusClass(status: string): string {
        switch (status) {
            case 'Active': return 'bg-blue-100 text-blue-700';
            case 'Closed': return 'bg-green-100 text-green-700';
            case 'Adjusted': return 'bg-purple-100 text-purple-700';
            default: return 'bg-gray-100 text-gray-700';
        }
    }
}
