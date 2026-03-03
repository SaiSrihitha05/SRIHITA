import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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

  claims: any[] = [];
  loading = true;
  selectedClaim: any = null;
  showProcessModal = false;

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
    // Reset form
    this.processForm = {
      status: 'Settled',
      remarks: '',
      settlementAmount: null
    };
    this.showProcessModal = true;
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

    this.claimService.processClaim(this.selectedClaim.id, dto).subscribe({
      next: () => {
        alert(`Claim status updated to ${this.processForm.status} successfully!`);
        this.closeModal();
        this.loadMyClaims();
      },
      error: (err) => {
        alert(err.error?.detail || 'Error updating claim status');
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