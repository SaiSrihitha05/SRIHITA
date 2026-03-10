import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ClaimService } from '../../../services/claim-service';
import { PolicyService } from '../../../services/policy-service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-file-claim',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './file-claim.html'
})
export class FileClaim implements OnInit {
  private route = inject(ActivatedRoute);
  private claimService = inject(ClaimService);
  private policyService = inject(PolicyService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  policy: any;
  selectedFiles: File[] = [];
  submitAttempted = false;

  // Preview fields
  selectedMemberObj: any = null;
  currentCoverage: number = 0;
  bonusDetails: any = null;
  totalClaimAmount: number = 0;
  planHasBonus: boolean = false;

  claimData = {
    policyAssignmentId: 0,
    policyMemberId: 0,
    claimType: 'Death',
    deathCertificateNumber: '',
    nomineeName: '',
    nomineeContact: '',
    remarks: ''
  };

  ngOnInit() {
    const policyId = Number(this.route.snapshot.queryParams['policyId']);

    if (!policyId || policyId === 0) {
      alert('No policy selected. Please go back and select a policy.');
      this.router.navigate(['/customer-dashboard/my-policies']);
      return;
    }

    this.policyService.getPolicyById(policyId).subscribe({
      next: (data) => {
        this.policy = data;
        this.claimData.policyAssignmentId = policyId;
        const primary = data.members.find((m: any) => m.isPrimaryInsured);
        if (primary) {
          this.claimData.policyMemberId = primary.id;
          this.onMemberSelected();
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading policy:', err);
        alert('Could not load policy details.');
        this.router.navigate(['/customer-dashboard/my-policies']);
        this.cdr.detectChanges();
      }
    });
  }

  onMemberSelected() {
    this.selectedMemberObj = this.policy.members.find(
      (m: any) => m.id == this.claimData.policyMemberId
    );

    if (this.selectedMemberObj) {
      this.currentCoverage = this.selectedMemberObj.currentCoverageAmount
        || this.selectedMemberObj.coverageAmount;

      this.bonusDetails = this.policy.bonusDetails;
      this.planHasBonus = this.policy.planHasBonus;

      // Total = coverage + bonus (Note: Terminal bonus is usually maturity only, so we stick to accumulated for death)
      this.totalClaimAmount = this.currentCoverage + (this.bonusDetails?.totalBonus ?? 0);
    }
  }

  onFileSelect(event: any) {
    this.selectedFiles = Array.from(event.target.files);
  }

  isClaimValid(): boolean {
    if (!this.claimData.policyMemberId) return false;

    if (this.claimData.claimType === 'Death') {
      return !!this.claimData.deathCertificateNumber && this.selectedFiles.length > 0;
    }

    return true;
  }

  submitClaim() {
    this.submitAttempted = true;

    if (!this.isClaimValid()) {
      return;
    }

    const formData = new FormData();
    formData.append('PolicyAssignmentId', this.claimData.policyAssignmentId.toString());
    formData.append('PolicyMemberId', this.claimData.policyMemberId.toString());
    formData.append('ClaimType', this.claimData.claimType);
    formData.append('Remarks', this.claimData.remarks);

    if (this.claimData.claimType === 'Death') {
      formData.append('DeathCertificateNumber', this.claimData.deathCertificateNumber);
    }

    this.selectedFiles.forEach(file =>
      formData.append('Documents', file)
    );

    this.claimService.fileClaim(formData).subscribe({
      next: () => {
        alert('Claim filed successfully!');
        this.router.navigate(['/customer-dashboard/my-claims']);
      },
      error: (err) => {
        alert(err.error?.detail || 'Error filing claim');
      }
    });
  }
}