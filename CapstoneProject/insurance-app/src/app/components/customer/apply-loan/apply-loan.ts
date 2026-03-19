import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { LoanService } from '../../../services/loan.service';
import { PolicyService } from '../../../services/policy-service';
import { ApplyLoanDto } from '../../../models/loan.model';

@Component({
  selector: 'app-apply-loan',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './apply-loan.html'
})
export class ApplyLoan implements OnInit {
    private loanService = inject(LoanService);
    private policyService = inject(PolicyService);
    private router = inject(Router);
    private cdr = inject(ChangeDetectorRef);

    allPolicies: any[] = [];
    selectedPolicyId: number | null = null;
    eligibilityResult: { eligible: boolean; reason: string; estimatedAmount: number; interestRate: number } | null = null;
    isApplying = false;

    ngOnInit() {
        this.fetchPolicies();
    }

    fetchPolicies() {
        this.policyService.getMyPolicies().subscribe(policies => {
            // Filter only active policies that have a loan interest rate defined (facility exists)
            this.allPolicies = policies.filter(p => p.status === 'Active');
            this.cdr.detectChanges();
        });
    }

    onPolicySelected() {
        if (!this.selectedPolicyId) {
            this.eligibilityResult = null;
            return;
        }

        const policy = this.allPolicies.find(p => p.id === this.selectedPolicyId);
        if (policy) {
            this.loanService.checkEligibility(policy.id).subscribe((result: { eligible: boolean; reason: string; estimatedAmount: number; interestRate: number }) => {
                this.eligibilityResult = result;
                this.cdr.detectChanges();
            });
        }
    }

    applyForLoan() {
        if (!this.selectedPolicyId || !this.eligibilityResult?.eligible) return;

        this.isApplying = true;
        const dto: ApplyLoanDto = {
            policyAssignmentId: this.selectedPolicyId
        };

        this.loanService.applyForLoan(dto).subscribe({
            next: (res) => {
                this.isApplying = false;
                alert(`✅ Loan of ₹${this.eligibilityResult?.estimatedAmount.toFixed(2)} approved instantly!`);
                this.router.navigate(['/customer-dashboard/my-loans']);
            },
            error: (err) => {
                this.isApplying = false;
                alert(`❌ Application Failed: ${err.error?.message || "Internal Error"}`);
            }
        });
    }
}
