import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { LoanService } from '../../../services/loan.service';
import { RepayLoanDto, LoanResponseDto } from '../../../models/loan.model';

@Component({
  selector: 'app-repay-loan',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './repay-loan.html'
})
export class RepayLoan implements OnInit {
    private loanService = inject(LoanService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private cdr = inject(ChangeDetectorRef);

    loan: LoanResponseDto | null = null;
    loanId: number = 0;
    
    repaymentForm = {
        amount: 0,
        paymentMethod: 'UPI' // Default
    };

    get isOverpaid(): boolean {
        return !!this.loan && this.repaymentForm.amount > this.loan.outstandingBalance;
    }

    get isValidAmount(): boolean {
        return this.repaymentForm.amount > 0 && !this.isOverpaid;
    }

    isProcessing = false;

    ngOnInit() {
        this.loanId = Number(this.route.snapshot.paramMap.get('id'));
        if (this.loanId) {
            this.fetchLoanDetails();
        }
    }

    fetchLoanDetails() {
        this.loanService.getMyLoans().subscribe(loans => {
            this.loan = loans.find(l => l.id === this.loanId) || null;
            if (this.loan) {
                this.repaymentForm.amount = this.loan.outstandingBalance;
            }
            this.cdr.detectChanges();
        });
    }

    submitRepayment() {
        if (!this.loan || this.repaymentForm.amount <= 0) return;

        if (this.repaymentForm.amount > this.loan.outstandingBalance) {
            alert(`Repayment cannot exceed outstanding balance of ₹${this.loan.outstandingBalance}`);
            return;
        }

        this.isProcessing = true;
        const dto: RepayLoanDto = {
            policyLoanId: this.loanId,
            amount: this.repaymentForm.amount,
            paymentMethod: this.repaymentForm.paymentMethod
        };

        this.loanService.repayLoan(dto).subscribe({
            next: (res) => {
                this.isProcessing = false;
                alert(`Payment of ₹${dto.amount} processed successfully!`);
                this.router.navigate(['/customer-dashboard/my-loans']);
            },
            error: (err) => {
                this.isProcessing = false;
                alert(`Repayment Failed: ${err.error?.message || "Internal Error"}`);
            }
        });
    }
}
