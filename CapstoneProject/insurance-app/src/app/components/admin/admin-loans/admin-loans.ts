import { Component, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoanService } from '../../../services/loan.service';
import { LoanResponseDto } from '../../../models/loan.model';

@Component({
    selector: 'app-admin-loans',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './admin-loans.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminLoans implements OnInit {
    private loanService = inject(LoanService);
    private cdr = inject(ChangeDetectorRef);

    loans: LoanResponseDto[] = [];
    loading = true;
    filter = 'All';

    get filtered() {
        if (this.filter === 'All') return this.loans;
        return this.loans.filter(l => l.status === this.filter);
    }

    get totalActive() {
        return this.loans.filter(l =>
            l.status === 'Active').length;
    }

    get totalOutstanding() {
        return this.loans
            .filter(l => l.status === 'Active')
            .reduce((sum, l) => sum + l.outstandingBalance, 0);
    }

    get totalDisbursed() {
        return this.loans.reduce((sum, l) =>
            sum + l.loanAmount, 0);
    }

    ngOnInit() {
        this.loanService.getAllLoans().subscribe({
            next: (data) => {
                this.loans = data;
                this.loading = false;
                this.cdr.markForCheck();
            },
            error: () => {
                this.loading = false;
                this.cdr.markForCheck();
            }
        });
    }

    setFilter(f: string) {
        this.filter = f;
        this.cdr.markForCheck();
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
