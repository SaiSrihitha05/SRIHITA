import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClaimService } from '../../../services/claim-service';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-my-claims',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './my-claims.html'
})
export class MyClaims implements OnInit {
  private claimService = inject(ClaimService);
  private cdr = inject(ChangeDetectorRef);

  claims: any[] = [];
  loading = true;
  selectedClaim: any = null;

  // Resubmission State
  showResubmitModal = false;
  resubmitClaimId: number | null = null;
  resubmitAmount: number = 0;
  resubmitRemarks: string = '';
  resubmitCategory: string | null = null;
  resubmitFiles: { [category: string]: File } = {};
  isSubmitting = false;

  ngOnInit() {
    this.loadClaims();
  }

  loadClaims() {
    this.loading = true;
    this.claimService.getMyClaims().subscribe({
      next: (data: any[]) => {
        this.claims = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error('Error fetching claims:', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Submitted':   return 'bg-blue-500';
      case 'UnderReview': return 'bg-amber-500';
      case 'Settled':     return 'bg-green-500';
      case 'Rejected':    return 'bg-red-500';
      case 'DocumentationRequested': return 'bg-purple-500';
      default:            return 'bg-gray-500';
    }
  }

  viewClaimDetails(claim: any) {
    this.selectedClaim = claim;
    this.cdr.detectChanges();
  }

  closeDetailsModal() {
    this.selectedClaim = null;
    this.cdr.detectChanges();
  }

  getLatestUpdate(claim: any): any {
    return claim.processedDate || claim.filedDate;
  }

  // Resubmission Helpers
  openResubmitForm(claim: any, category: string | null = null) {
    this.resubmitClaimId = claim.id;
    this.resubmitAmount = claim.claimAmount;
    this.resubmitRemarks = '';
    this.resubmitFiles = {};
    this.resubmitCategory = category;
    this.showResubmitModal = true;
    
    // If opening for a specific category, we don't necessarily close the details, 
    // but usually, it's cleaner to focus on the form.
    // this.selectedClaim = null; 
    this.cdr.detectChanges();
  }

  handleFileInput(event: any, category: string = 'ClaimDocument') {
    if (event.target.files.length > 0) {
      this.resubmitFiles[category] = event.target.files[0];
    }
  }

  submitResubmission() {
    if (!this.resubmitClaimId || this.isSubmitting) return;

    this.isSubmitting = true;
    const formData = new FormData();
    formData.append('claimAmount', this.resubmitAmount.toString());
    formData.append('remarks', this.resubmitRemarks);
    
    // Append files and their categories
    Object.keys(this.resubmitFiles).forEach(category => {
      formData.append('documents', this.resubmitFiles[category]);
      formData.append('documentCategories', category);
    });

    this.claimService.resubmitClaim(this.resubmitClaimId, formData).subscribe({
      next: () => {
        this.showResubmitModal = false;
        this.isSubmitting = false;
        this.loadClaims();
        alert('Claim resubmitted successfully!');
      },
      error: (err) => {
        console.error('Error resubmitting claim:', err);
        alert('Failed to resubmit claim: ' + (err.error?.message || 'Unknown error'));
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  isPastDeadline(claim: any): boolean {
    if (!claim.resubmissionDeadline) return false;
    return new Date() > new Date(claim.resubmissionDeadline);
  }
}