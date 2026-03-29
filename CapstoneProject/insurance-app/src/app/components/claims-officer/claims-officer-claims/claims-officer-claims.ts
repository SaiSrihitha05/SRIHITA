import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
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
  private route = inject(ActivatedRoute);

  highlightedClaimId: number | null = null;

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
    rejectionReason: '',
    settlementAmount: null as number | null
  };

  loadingDetail = false;

  ngOnInit() {
    this.loadMyClaims();

    const highlightId = this.route.snapshot.queryParamMap.get('claimId');
    if (highlightId) {
      this.highlightedClaimId = parseInt(highlightId);
    }
  }

  scrollToHighlighted() {
    if (!this.highlightedClaimId) return;
    const el = document.getElementById(`claim-row-${this.highlightedClaimId}`);
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  loadMyClaims() {
    this.loading = true;
    this.claimService.getMyAssignedClaims().subscribe({
      next: (data) => {
        this.claims = data;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.highlightedClaimId) {
          setTimeout(() => this.scrollToHighlighted(), 500);
        }
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
          rejectionReason: fullClaim.rejectionReason || '',
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

    if (this.processForm.status === 'Rejected' && !this.processForm.rejectionReason.trim()) {
      alert('Rejection reason is required');
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

    if (this.processForm.status === 'Rejected') {
      dto.rejectionReason = this.processForm.rejectionReason;
    }

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

        // 3. Navigate/Refresh
        if (this.router.url !== '/claims-officer-dashboard/my-claims') {
          this.router.navigate(['/claims-officer-dashboard/my-claims']).then(() => {
            this.loadMyClaims();
          });
        } else {
          this.loadMyClaims();
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitting = false;
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
      case 'Resubmitted': return 'bg-indigo-500 animate-pulse';
      case 'Rejected': return 'bg-red-500';
      case 'PermanentlyRejected': return 'bg-red-900';
      default: return 'bg-gray-500';
    }
  }

  canProcess(claim: any): boolean {
    // Disable processing for final states: Settled or Rejected (Permanently)
    return claim.status !== 'Settled' && claim.status !== 'PermanentlyRejected';
  }
  
  downloadDocument(doc: any) {
    if (!this.selectedClaim) return;
    
    this.claimService.downloadClaimDocument(this.selectedClaim.id, doc.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.fileName;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      },
      error: (err) => {
        console.error('Error downloading document:', err);
        alert('Failed to download document: ' + (err.error?.message || 'Unknown error'));
      }
    });
  }
}