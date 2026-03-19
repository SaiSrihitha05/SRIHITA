import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClaimService } from '../../../services/claim-service';
import { PolicyService } from '../../../services/policy-service';

@Component({
  selector: 'app-claims-officer-claims',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './claims-officer-claims.html'
})
export class ClaimsOfficerClaims implements OnInit {
  private claimService = inject(ClaimService);
  private policyService = inject(PolicyService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  claims: any[] = [];
  loading = true;
  selectedClaim: any = null;
  selectedClaimPolicy: any = null;
  showProcessModal = false;
  submitting = false;
  readonly apiUrl = 'https://localhost:7027/api/Claims';

  // All statuses from enum for full officer flexibility
  statusOptions = [
    { label: 'Settle Claim', value: 'Settled' },
    { label: 'Reject Claim', value: 'Rejected' }
  ];

  // Process form data
  processForm = {
    status: 'Settled',
    remarks: '',
    settlementAmount: null as number | null
  };

  loadingDetail = false;

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
    this.loadingDetail = true;
    this.showProcessModal = true;
    this.selectedClaim = claim; // Temporary while loading full details
    this.cdr.detectChanges();

    this.claimService.getClaimById(claim.id).subscribe({
      next: (fullClaim) => {
        this.selectedClaim = fullClaim;
        this.loadingDetail = false;
        
        // Suggest status and amount
        this.processForm = {
          status: 'Settled',
          remarks: fullClaim.remarks || '',
          settlementAmount: fullClaim.netSettlementAmount > 0 ? fullClaim.netSettlementAmount : fullClaim.claimAmount
        };

        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingDetail = false;
        this.cdr.detectChanges();
      }
    });
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

        // 3. Navigate conditionally to avoid "Navigation failed"
        if (this.router.url !== '/claims-officer-dashboard/my-claims') {
          this.router.navigate(['/claims-officer-dashboard/my-claims']).then(navigated => {
            if (navigated) {
              console.log('Navigation successful');
            }
            this.loadMyClaims();
          });
        } else {
          // Already on the page, just refresh data
          this.loadMyClaims();
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitting = false;
        // Show detailed error if available from ProblemDetails (ASP.NET Core standard)
        const errorMsg = err.error?.detail || err.error?.title || 'Error updating claim status';
        alert(`Failed to Process Claim: ${errorMsg}`);
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