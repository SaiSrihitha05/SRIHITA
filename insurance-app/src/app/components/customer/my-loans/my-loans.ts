import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoanService } from '../../../services/loan.service';
import { PolicyService } from '../../../services/policy-service'; // ✅ ADDED
import { LoanResponseDto } from '../../../models/loan.model';
import { RouterModule } from '@angular/router'; // ✅ ADDED


@Component({
    selector: 'app-my-loans',
    standalone: true,
    imports: [CommonModule, RouterModule], // ✅ ADDED
    templateUrl: './my-loans.html'
})
export class MyLoans implements OnInit {
    private loanService = inject(LoanService);
    private policyService = inject(PolicyService); // ✅ ADDED
    private cdr = inject(ChangeDetectorRef);

    loans: LoanResponseDto[] = [];
    policies: any[] = []; // ✅ ADDED
    loading = true;

    ngOnInit() {
        this.fetchLoans();
        this.fetchPolicies(); // ✅ ADDED
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

    fetchPolicies() {
        this.policyService.getMyPolicies().subscribe({
            next: (data) => {
                this.policies = data.filter(p => p.status === 'Active');
                this.cdr.detectChanges();
            }
        });
    }

    isEligible(policy: any): { eligible: boolean, reason?: string, amount?: number } {
        if (!policy.planHasLoanFacility) {
            return { eligible: false, reason: 'Plan does not support loans' };
        }

        const startDate = new Date(policy.startDate);
        const today = new Date();
        const yearsDiff = today.getFullYear() - startDate.getFullYear();

        if (yearsDiff < policy.planLoanEligibleAfterYears) {
            return {
                eligible: false,
                reason: `Eligible after ${policy.planLoanEligibleAfterYears} years (Policy age: ${yearsDiff} yr)`
            };
        }

        // Check if already has an active loan
        const hasActiveLoan = this.loans.some(l => l.policyAssignmentId === policy.id && l.status === 'Active');
        if (hasActiveLoan) {
            return { eligible: false, reason: 'Active loan already exists' };
        }

        // Potential amount calculation (mocked formula from backend: 30% of paid * maxLoan%)
        // Since we don't have all payments here easily, we show "Eligible"
        return { eligible: true };
    }

    repayLoan(loanId: number, currentBalance: number) {
        const amountStr = prompt(`Enter repayment amount (Current Balance: ₹${currentBalance})`);
        if (!amountStr) return;

        const amount = parseFloat(amountStr);
        if (isNaN(amount) || amount <= 0) {
            alert('Invalid amount');
            return;
        }

        // Allow up to currentBalance + ~2% for interest buffer in prompt, but let backend validate exactly
        if (amount > currentBalance * 1.05) {
            if (!confirm(`Amount ₹${amount} exceeds principal ₹${currentBalance}. Include accrued interest?`)) return;
        }

        this.loanService.repayLoan({ policyLoanId: loanId, amount }).subscribe({
            next: () => {
                alert('Repayment successful!');
                this.fetchLoans();
            },
            error: (err) => alert(err.error?.message || 'Repayment failed')
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
