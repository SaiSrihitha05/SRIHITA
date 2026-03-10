import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClaimService } from '../../../services/claim-service';

@Component({
  selector: 'app-claims-officer-claims',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './claims-officer-claims.html'
})
export class ClaimsOfficerClaims implements OnInit {
  private claimService = inject(ClaimService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  claims: any[] = [];
  loading = true;
  selectedClaim: any = null;
  showProcessModal = false;
  submitting = false;

  // All statuses from enum for full officer flexibility
  statusOptions = [
    { label: 'Submit', value: 'Submitted' },
    { label: 'Reviewing', value: 'UnderReview' },
    { label: 'Approve', value: 'Approved' },
    { label: 'Reject', value: 'Rejected' },
    { label: 'Settle', value: 'Settled' }
  ];

  // Process form data
  processForm = {
    status: 'Settled',
    remarks: '',
    settlementAmount: null as number | null
  };

  ngOnInit() {
    this.loadMyClaims();
  }

  loadMyClaims() {
    this.loading = true;
    this.claimService.getMyAssignedClaims().subscribe({
      next: (data) => {
        this.claims = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openProcessModal(claim: any) {
    this.selectedClaim = claim;
    // Suggest net amount if loan exists
    const suggestedAmount = claim.claimAmount - (claim.outstandingLoanAmount || 0);

    // Reset form
    this.processForm = {
      status: 'Settled',
      remarks: '',
      settlementAmount: suggestedAmount > 0 ? suggestedAmount : claim.claimAmount
    };
    this.showProcessModal = true;
    this.submitting = false;
    this.cdr.detectChanges();
  }

  closeModal() {
    this.showProcessModal = false;
    this.selectedClaim = null;
    this.cdr.detectChanges();
  }

  submitProcess() {
    if (!this.processForm.remarks.trim()) {
      alert('Remarks are required');
      return;
    }

    // Settlement amount required when Approved or Settled
    const isSettlementNeeded = this.processForm.status === 'Approved' || this.processForm.status === 'Settled';
    if (isSettlementNeeded &&
      (!this.processForm.settlementAmount ||
        this.processForm.settlementAmount <= 0)) {
      alert('Settlement amount is required when approving or settling a claim');
      return;
    }

    const dto: any = {
      status: this.processForm.status,
      remarks: this.processForm.remarks
    };

    if (isSettlementNeeded) {
      dto.settlementAmount = this.processForm.settlementAmount;
    }

    this.submitting = true;
    this.claimService.processClaim(this.selectedClaim.id, dto).subscribe({
      next: () => {
        // 1. Close modal first to clean up the DOM
        this.showProcessModal = false;
        this.selectedClaim = null;

        // 2. Stop the loading spinner
        this.submitting = false;

        // 3. Navigate BEFORE any alerts (Direct navigation as requested)
        this.router.navigate(['/claims-officer-dashboard/my-claims']).then(navigated => {
          if (navigated) {
            console.log('Navigation successful');
          } else {
            console.error('Navigation failed. Check if the route exists in your AppRoutingModule');
          }
          // Always refresh data if we stayed on same page or navigated successfully
          this.loadMyClaims();
        });

        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitting = false;
        alert(err.error?.detail || 'Error updating claim status');
        this.cdr.detectChanges();
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Submitted': return 'bg-blue-500';
      case 'UnderReview': return 'bg-amber-500';
      case 'Approved': return 'bg-green-400';
      case 'Settled': return 'bg-green-600';
      case 'Rejected': return 'bg-red-500';
      default: return 'bg-gray-500';
    }
  }

  canProcess(claim: any): boolean {
    // Disable processing for final states: Settled or Rejected
    return claim.status !== 'Settled' && claim.status !== 'Rejected';
  }
}