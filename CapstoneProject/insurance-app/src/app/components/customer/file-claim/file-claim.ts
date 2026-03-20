import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ClaimService } from '../../../services/claim-service';
import { KycService } from '../../../services/kyc-service';
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
  private kycService = inject(KycService);
  private policyService = inject(PolicyService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  allPolicies: any[] = [];
  customerClaims: any[] = [];
  policy: any;
  selectedFiles: File[] = [];
  submitAttempted = false;
  isLoading = true;

  // OCR Verification for Death Certificate
  deathCertificateVerification: 'Pending' | 'Processing' | 'Verified' | 'Failed' = 'Pending';
  deathCertError: string = '';
  verificationRes: any = null;

  // Nominee Identity Verification (Filer)
  filerNomineeId: number | null = null;
  nomineeVerification: 'Pending' | 'Processing' | 'Verified' | 'Failed' = 'Pending';
  nomineeCertError: string = '';

  // Eligibility Feedback
  eligibilityResult: { isEligible: boolean; reason: string } | null = null;

  // Selection
  selectedPolicyId: number | null = null;
  
  // Preview fields
  selectedMemberObj: any = null;
  currentCoverage: number = 0;
  bonusDetails: any = null;
  totalClaimAmount: number = 0;
  planHasBonus: boolean = false;

  claimData = {
    policyAssignmentId: 0,
    claimForMemberId: 0,
    claimType: 'Death',
    deathCertificateNumber: '',
    dateOfDeath: null,
    causeOfDeath: '',
    placeOfDeath: '',
    remarks: ''
  };

  get eligiblePolicies() {
    return this.allPolicies.filter(p => {
      // Basic check: Policy must be Active
      if (p.status !== 'Active') return false;
      
      // Smart check: Must have at least one member not already claimed
      const memberIdsWithClaims = this.customerClaims
        .filter((c: any) => c.policyAssignmentId === p.id && c.status !== 'Rejected')
        .map((c: any) => c.claimForMemberId);

      const hasUnclaimedMember = p.members?.some((m: any) => 
        m.status === 'Active' && !memberIdsWithClaims.includes(m.id)
      );

      return hasUnclaimedMember;
    });
  }

  get activeMembers() {
    return this.policy?.members?.filter((m: any) => m.status === 'Active') || [];
  }

  ngOnInit() {
    const queryPolicyId = Number(this.route.snapshot.queryParams['policyId']);

    // Load necessary data in parallel
    this.isLoading = true;
    this.policyService.getMyPolicies().subscribe({
      next: (policies) => {
        this.allPolicies = policies;
        
        this.claimService.getMyClaims().subscribe({
          next: (claims) => {
            this.customerClaims = claims;
            this.isLoading = false;

            if (queryPolicyId) {
              this.selectedPolicyId = queryPolicyId;
              this.onPolicySelected();
            } else if (this.eligiblePolicies.length === 1) {
              this.selectedPolicyId = this.eligiblePolicies[0].id;
              this.onPolicySelected();
            }
            this.cdr.detectChanges();
          },
          error: () => this.isLoading = false
        });
      },
      error: () => this.isLoading = false
    });
  }

  onPolicySelected() {
    if (!this.selectedPolicyId) {
      this.policy = null;
      this.eligibilityResult = null;
      return;
    }

    this.policyService.getPolicyById(this.selectedPolicyId).subscribe({
      next: (data) => {
        this.policy = data;
        this.claimData.policyAssignmentId = this.selectedPolicyId!;
        
        // Auto-select first eligible member
        const eligibleMember = this.activeMembers.find((m: any) => this.isMemberEligible(m).isEligible);
        if (eligibleMember) {
          this.claimData.claimForMemberId = eligibleMember.id;
          this.onMemberSelected();
        } else {
          this.claimData.claimForMemberId = 0;
          this.eligibilityResult = { isEligible: false, reason: 'No eligible members found in this policy.' };
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading policy:', err);
        alert('Could not load policy details.');
      }
    });
  }

  isMemberEligible(member: any): { isEligible: boolean; reason: string } {
    if (!member) return { isEligible: false, reason: 'No member selected.' };
    if (member.status !== 'Active') return { isEligible: false, reason: 'Member status is not Active.' };

    // Check if a claim already exists for this member
    const existingClaim = this.customerClaims.find(c => 
      c.policyAssignmentId === this.policy.id && 
      c.claimForMemberId === member.id &&
      c.status !== 'Rejected'
    );

    if (existingClaim) {
      return { 
        isEligible: false, 
        reason: `A claim is already ${existingClaim.status.toLowerCase()} for this member.` 
      };
    }

    return { isEligible: true, reason: 'Eligible for claim' };
  }

  onMemberSelected() {
    this.selectedMemberObj = this.policy?.members?.find(
      (m: any) => m.id == this.claimData.claimForMemberId
    );

    if (this.selectedMemberObj) {
      this.currentCoverage = this.selectedMemberObj.currentCoverageAmount
        || this.selectedMemberObj.coverageAmount;

      this.bonusDetails = this.policy.bonusDetails;
      this.planHasBonus = this.policy.planHasBonus;

      // Total = coverage + bonus
      this.totalClaimAmount = this.currentCoverage + (this.bonusDetails?.totalBonus ?? 0);
    }
  }

  getMemberPayout(nomineeShare: number): number {
    return (this.totalClaimAmount * nomineeShare) / 100;
  }

  onFileSelect(event: any) {
    this.selectedFiles = Array.from(event.target.files);
    
    // Auto-trigger OCR if it's a death claim and certificate number is present
    if (this.claimData.claimType === 'Death' && this.claimData.deathCertificateNumber && this.selectedFiles.length > 0) {
      this.performDeathCertOcr();
    }
  }

  performDeathCertOcr() {
    if (!this.claimData.deathCertificateNumber || !this.claimData.dateOfDeath || this.selectedFiles.length === 0) return;

    this.deathCertificateVerification = 'Processing';
    this.deathCertError = '';
    this.cdr.detectChanges();

    const file = this.selectedFiles[0]; // Assuming first file is the certificate
    this.kycService.verifyDeathCertificate(file, this.claimData.deathCertificateNumber, this.claimData.dateOfDeath, this.selectedMemberObj.memberName).subscribe({
      next: (res: any) => {
        this.deathCertificateVerification = res.isSuccess ? 'Verified' : 'Failed';
        this.verificationRes = res;
        if (!res.isSuccess) this.deathCertError = res.message;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.deathCertificateVerification = 'Failed';
        this.deathCertError = 'OCR Service error. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  performNomineeOcr(event: any) {
    const file = event.target.files[0];
    if (!file || !this.filerNomineeId) return;

    const nominee = this.policy.nominees.find((n: any) => n.id == this.filerNomineeId);
    if (!nominee) return;

    this.nomineeVerification = 'Processing';
    this.nomineeCertError = '';
    this.cdr.detectChanges();

    this.kycService.verifyNomineeIdentity(file, nominee.nomineeName).subscribe({
      next: (res: any) => {
        this.nomineeVerification = res.isSuccess ? 'Verified' : 'Failed';
        if (!res.isSuccess) {
          this.nomineeCertError = res.message;
        }
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.nomineeVerification = 'Failed';
        this.nomineeCertError = 'Verification service error.';
        this.cdr.detectChanges();
      }
    });
  }

  resetDeathKyc() {
    if (this.deathCertificateVerification === 'Pending' && !this.verificationRes) return;
    
    this.deathCertificateVerification = 'Pending';
    this.deathCertError = '';
    this.verificationRes = null;
    this.cdr.detectChanges();
  }

  validateDeathCertNumber() {
    this.resetDeathKyc();
    const val = this.claimData.deathCertificateNumber;
    if (val && val.length < 4) {
      this.deathCertError = 'Certificate number too short.';
    } else if (val && !/^[A-Z0-9\-/]+$/i.test(val)) {
      this.deathCertError = 'Invalid characters in certificate number.';
    } else {
      this.deathCertError = '';
    }
    this.cdr.detectChanges();
  }

  isClaimValid(): boolean {
    if (!this.claimData.claimForMemberId) return false;

    if (this.claimData.claimType === 'Death') {
      return !!this.claimData.deathCertificateNumber && 
             this.selectedFiles.length > 0 && 
             this.deathCertificateVerification === 'Verified' &&
             this.nomineeVerification === 'Verified';
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
    formData.append('ClaimForMemberId', this.claimData.claimForMemberId.toString());
    formData.append('ClaimType', this.claimData.claimType);
    formData.append('Remarks', this.claimData.remarks);

    if (this.claimData.claimType === 'Death') {
      formData.append('DeathCertificateNumber', this.claimData.deathCertificateNumber);
      if (this.claimData.dateOfDeath) formData.append('DateOfDeath', this.claimData.dateOfDeath);
      formData.append('CauseOfDeath', this.claimData.causeOfDeath);
      formData.append('PlaceOfDeath', this.claimData.placeOfDeath);
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