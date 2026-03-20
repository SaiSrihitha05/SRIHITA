import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PolicyService } from '../../../services/policy-service';
import { PlanService } from '../../../services/plan-service';
import { UserService } from '../../../services/user-service';
import { KycService } from '../../../services/kyc-service';

@Component({
  selector: 'app-buy-policy',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './buy-policy.html'
})
export class BuyPolicy implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private policyService = inject(PolicyService);
  private planService = inject(PlanService);
  private userService = inject(UserService);
  private kycService = inject(KycService);
  private cdr = inject(ChangeDetectorRef);

  step = 1;
  planId: number = 0;
  selectedPlan: any = null;
  draftId: number | null = null;
  isSavingDraft = false;
  processing: boolean = false;
  today: string = '';
  yesterday: string = '';
  customerProfile: any = null;

  // Section 1: Basic Config (Matching PolicyService.cs logic)
  numMembers: number = 1;
  numNominees: number = 1;
  policyData = {
    startDate: '',
    premiumFrequency: 'Monthly', // Options: Monthly, Quarterly, Yearly
    termYears: 0
  };

  members: any[] = [];
  nominees: any[] = [];

  identityProof: File | null = null;
  incomeProof: File | null = null;
  memberDocuments: File[] = [];

  get isWholeLifePlan(): boolean {
    return !!this.selectedPlan?.isCoverageUntilAge;
  }

  ngOnInit() {
    const now = new Date();
    // Helper to get local date string YYYY-MM-DD
    const toLocaleISO = (d: Date) => {
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    this.today = toLocaleISO(now);
    const yest = new Date(now);
    yest.setDate(now.getDate() - 1);
    this.yesterday = toLocaleISO(yest);

    const planIdParam = this.route.snapshot.queryParams['planId'];
    const draftIdParam = this.route.snapshot.queryParams['draftId'];

    if (draftIdParam) {
      this.loadDraft(Number(draftIdParam));
    } else if (planIdParam) {
      this.planId = Number(planIdParam);
      this.loadPlan(this.planId);
    }
    this.loadUserProfile();
  }

  loadUserProfile() {
    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.customerProfile = profile;
        if (this.members[0] && this.members[0].RelationshipToCustomer === 'Self') {
          this.syncSelfMember();
        }
      }
    });
  }

  loadPlan(planId: number) {
    this.planService.getPlanById(planId).subscribe(plan => {
      this.selectedPlan = plan;
      if (this.isWholeLifePlan) {
        // For lifelong/whole life, term is Target Age - Entry Age
        const primaryAge = this.members[0] ? this.getAge(this.members[0].DateOfBirth) : 30;
        this.policyData.termYears = Math.max(1, (this.selectedPlan.coverageUntilAge || 100) - primaryAge);
      } else if (!this.policyData.termYears) {
        this.policyData.termYears = plan.minTermYears;
      }
      this.syncMembers();
      this.syncNominees();
    });
  }

  loadDraft(draftId: number) {
    this.policyService.getPolicyById(draftId).subscribe({
      next: (draft) => {
        this.draftId = draft.id;
        this.planId = draft.planId;

        // Load the plan details for constraints and display
        if (this.planId) {
          this.loadPlan(this.planId);
        }

        this.policyData.startDate = draft.startDate
          ? draft.startDate.split('T')[0]
          : '';
        this.policyData.termYears = draft.termYears || 
          (this.isWholeLifePlan ? 0 : this.selectedPlan?.minTermYears);
        this.policyData.premiumFrequency = draft.premiumFrequency || 'Monthly';

        // Restore members
        if (draft.members && draft.members.length > 0) {
          this.numMembers = draft.members.length;
          this.members = draft.members.map((m: any) => ({
            MemberName: m.memberName,
            RelationshipToCustomer: m.relationshipToCustomer,
            DateOfBirth: m.dateOfBirth
              ? m.dateOfBirth.split('T')[0]
              : '',
            Gender: m.gender,
            CoverageAmount: m.coverageAmount,
            IsSmoker: m.isSmoker,
            HasPreExistingDiseases: m.hasPreExistingDiseases,
            DiseaseDescription: m.diseaseDescription || '',
            Occupation: m.occupation,
            IsPrimaryInsured: m.isPrimaryInsured,
            IdProofType: m.idProofType || 'Aadhar Card',
            IdProofNumber: m.idProofNumber || '',
            KycStatus: m.kycVerificationStatus || 'Pending',
            ExtractedData: m.extractedIdNumber ? { isSuccess: true, extractedName: m.extractedName, extractedIdNumber: m.extractedIdNumber } : null,
            IdError: ''
          }));
        }

        // Restore nominees
        if (draft.nominees && draft.nominees.length > 0) {
          this.numNominees = draft.nominees.length;
          this.nominees = draft.nominees.map((n: any) => ({
            NomineeName: n.nomineeName,
            RelationshipToPolicyHolder: n.relationshipToPolicyHolder,
            ContactNumber: n.contactNumber,
            SharePercentage: n.sharePercentage
          }));
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading draft:', err);
      }
    });
  }

  syncMembers() {
    const current = this.members.length;
    if (this.numMembers > current) {
      for (let i = current; i < this.numMembers; i++) {
        this.members.push({
          MemberName: '', RelationshipToCustomer: i === 0 ? 'Self' : '',
          DateOfBirth: '', Gender: 'Male', CoverageAmount: this.selectedPlan.minCoverageAmount,
          IsSmoker: false, HasPreExistingDiseases: false, DiseaseDescription: '',
          Occupation: '', IsPrimaryInsured: i === 0,
           IdProofType: 'Aadhar Card', IdProofNumber: '',
           KycStatus: 'Pending', // Pending, Processing, Verified, Failed
           ExtractedData: null as any,
           IdError: ''
         });
      }
    } else { this.members.splice(this.numMembers); }
    
    if (this.members[0] && !this.members[0].MemberName && this.customerProfile) {
      this.syncSelfMember();
    }
    this.cdr.detectChanges();
  }

  onRelationshipChange(index: number) {
    const member = this.members[index];
    if (member.RelationshipToCustomer === 'Self') {
      this.syncSelfMember(index);
    } else if (this.customerProfile) {
      // If it was previously 'Self' (matching customer profile), clear the fields
      if (member.MemberName === this.customerProfile.name &&
          member.DateOfBirth === (this.customerProfile.dateOfBirth?.split('T')[0] || '') &&
          member.Gender === (this.customerProfile.gender || 'Male')) {
        member.MemberName = '';
        member.DateOfBirth = '';
        member.Gender = '';
      }
    }
  }

  syncSelfMember(index: number = 0) {
    if (!this.customerProfile) return;
    const member = this.members[index];
    member.MemberName = this.customerProfile.name;
    member.DateOfBirth = this.customerProfile.dateOfBirth?.split('T')[0] || '';
    member.Gender = this.customerProfile.gender || 'Male';
    this.cdr.detectChanges();
  }

  isSelfAlreadySelected(currentIndex: number): boolean {
    return this.members.some((m, i) => i !== currentIndex && m.RelationshipToCustomer === 'Self');
  }

  syncNominees() {
    const current = this.nominees.length;
    if (this.numNominees > current) {
      for (let i = current; i < this.numNominees; i++) {
        this.nominees.push({ NomineeName: '', RelationshipToPolicyHolder: '', ContactNumber: '', SharePercentage: 0 });
      }
    } else { this.nominees.splice(this.numNominees); }
    this.cdr.detectChanges();
  }

  getAge(dob: string): number {
    if (!dob) return 0;
    const birth = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    if (today < new Date(today.getFullYear(), birth.getMonth(), birth.getDate())) age--;
    return age;
  }
  saveOrUpdateDraft() {
    this.isSavingDraft = true;
    const freqMap: any = { 'Monthly': 0, 'Quarterly': 1, 'Yearly': 2 };

    const draftData = {
      planId: this.planId || null,
      startDate: this.policyData.startDate || null,
      termYears: this.policyData.termYears || 0,
      // Convert string to enum value
      premiumFrequency: freqMap[this.policyData.premiumFrequency] ?? 0,

      // Only send members/nominees if they have at least a Name
      members: this.members.filter(m => m.MemberName).map(m => ({
        ...m,
        DateOfBirth: m.DateOfBirth || null,
        RelationshipToCustomer: m.RelationshipToCustomer || null,
        Gender: m.Gender || null,
        Occupation: m.Occupation || null
      })),
      nominees: this.nominees.filter(n => n.NomineeName).map(n => ({
        ...n,
        RelationshipToPolicyHolder: n.RelationshipToPolicyHolder || null,
        ContactNumber: n.ContactNumber || null,
        SharePercentage: n.SharePercentage || 0
      }))
    };

    console.log("Sending Draft Payload:", draftData); // Useful for debugging the 400 error

    if (this.draftId) {
      this.policyService.updateDraft(this.draftId, draftData).subscribe({
        next: () => { this.isSavingDraft = false; this.cdr.detectChanges(); },
        error: (err) => { this.handleError(err); }
      });
    } else {
      this.policyService.saveDraft(draftData).subscribe({
        next: (res) => { this.draftId = res.id; this.isSavingDraft = false; this.cdr.detectChanges(); },
        error: (err) => { this.handleError(err); }
      });
    }
  }

  // Add this helper to see the EXACT validation errors from .NET
  handleError(err: any) {
    this.isSavingDraft = false;
    if (err.status === 400 && err.error.errors) {
      console.error("VALIDATION ERRORS:", err.error.errors);

    }
  }
  goBack() {
    this.router.navigate(['/customer-dashboard/explore-plans']);
  }

  saveAndExit() {
    this.isSavingDraft = true;
    const draftData = {
      planId: this.planId,
      startDate: this.policyData.startDate,
      termYears: this.policyData.termYears,
      premiumFrequency: this.policyData.premiumFrequency,
      members: this.members.map(m => ({
        ...m,
        DateOfBirth: m.DateOfBirth || null
      })),
      nominees: this.nominees.map(n => ({
        ...n,
        SharePercentage: n.SharePercentage || 0
      }))
    };

    const request = this.draftId
      ? this.policyService.updateDraft(this.draftId, draftData)
      : this.policyService.saveDraft(draftData);

    request.subscribe({
      next: () => {
        this.isSavingDraft = false;
        this.router.navigate(['/customer-dashboard/my-policies']);
      },
      error: () => {
        this.isSavingDraft = false;
        alert('Could not save draft. Please try again.');
      }
    });
  }

  getTotalNomineeShare(): number {
    return this.nominees.reduce((sum, n) => sum + (n.SharePercentage || 0), 0);
  }

  isFutureDate(): boolean {
    if (!this.policyData.startDate) {
      console.log("Step 1 Fail: No date selected.");
      return false; // Empty date is NOT a valid future date
    }

    const selected = new Date(this.policyData.startDate);
    const today = new Date();

    selected.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);

    return selected >= today;
  }

  isFormValid(): boolean {
    if (!this.selectedPlan) {
      console.warn("Validation Failed: No plan selected.");
      return false;
    }

    // --- Common Step 1 Validations (Always checked) ---
    const isDateValid = this.isFutureDate();
    const isTermValid = this.isWholeLifePlan ? true : (
      this.policyData.termYears >= this.selectedPlan.minTermYears &&
      this.policyData.termYears <= this.selectedPlan.maxTermYears
    );

    // Strict Number Guards: Must be at least 1 and within plan limits
    const isMemberCountValid = this.numMembers >= 1 &&
      this.numMembers <= this.selectedPlan.maxPolicyMembersAllowed;
    const isNomineeCountValid = this.numNominees >= this.selectedPlan.minNominees &&
      this.numNominees <= this.selectedPlan.maxNominees;

    // Log failures for Phase 1
    if (!isDateValid) console.log("Step 1 Fail: Invalid Start Date");
    if (!isTermValid) console.log(`Step 1 Fail: Term ${this.policyData.termYears} out of range`);
    if (!isMemberCountValid) console.log(`Step 1 Fail: Member count ${this.numMembers} is invalid`);
    if (!isNomineeCountValid) console.log(`Step 1 Fail: Nominee count ${this.numNominees} is invalid`);

    // --- Step-Specific Checks ---
    if (this.step === 1) {
      const step1Result = isDateValid && isTermValid && isMemberCountValid && isNomineeCountValid;
      if (step1Result) console.log("%c Step 1 Passed!", "color: green");
      return step1Result;
    }

    if (this.step === 2) {
      const areMembersValid = this.members.every((m, i) => {
        const age = this.getAge(m.DateOfBirth);
        const hasBasicInfo = m.MemberName && m.Occupation && m.DateOfBirth;
        const hasValidAge = age >= this.selectedPlan.minAge && age <= this.selectedPlan.maxAge;
        const hasValidCoverage = m.CoverageAmount >= this.selectedPlan.minCoverageAmount &&
          m.CoverageAmount <= this.selectedPlan.maxCoverageAmount;

        // Check for member-specific document uploads
        const hasDocument = m.IsPrimaryInsured ? !!this.identityProof : !!this.memberDocuments[i];
        
        // KYC Verification Guard: Must be 'Verified'
        const isKycVerified = m.KycStatus === 'Verified';
        if (!isKycVerified) {
          console.warn(`Member ${i} Fail: KYC not verified (Current Status: ${m.KycStatus})`);
        }
        
        // Relationship Validation
        const isSelf = m.RelationshipToCustomer === 'Self';
        const nameMatchesProfile = m.MemberName?.toLowerCase() === this.customerProfile?.name?.toLowerCase();
        
        const isRelationshipConsistent = isSelf ? nameMatchesProfile : !nameMatchesProfile;
        if (!isRelationshipConsistent && isSelf) console.log(`Member ${i} Fail: Self relation but name mismatch`);
        if (!isRelationshipConsistent && !isSelf) console.log(`Member ${i} Fail: Non-self relation but name matches customer`);

        return hasBasicInfo && hasValidAge && hasValidCoverage && hasDocument && isRelationshipConsistent && isKycVerified;
      });

      // Income proof is mandatory for the policy holder at Step 2
      const hasIncomeProof = !!this.incomeProof;

      return areMembersValid && hasIncomeProof;
    }

    if (this.step === 3) {

      const areNomineesValid = this.nominees.every(n =>
        n.NomineeName &&
        n.RelationshipToPolicyHolder &&
        n.ContactNumber &&
        n.SharePercentage > 0
      );

      const shareTotal = this.getTotalNomineeShare();
      const isShareValid = shareTotal === 100;

      if (!areNomineesValid) console.log("Step 3 Fail: Missing nominee details");
      if (!isShareValid) console.log(`Step 3 Fail: Nominee share is ${shareTotal}%`);

      return areNomineesValid && isShareValid;
    }

    if (this.step === 4) {
      return true;
    }

    return false;
  }
  onFileChange(event: any, type: string, index?: number) {
    const file = event.target.files[0];
    if (type === 'id') {
      this.identityProof = file;
      this.performKycCheck(0); // Primary insured is always index 0
    }
    if (type === 'income') this.incomeProof = file;
    if (type === 'member' && index !== undefined) {
      this.memberDocuments[index] = file;
      this.performKycCheck(index);
    }
    this.cdr.detectChanges();
  }

  performKycCheck(index: number) {
    const m = this.members[index];
    const file = m.IsPrimaryInsured ? this.identityProof : this.memberDocuments[index];

    if (!file || !m.IdProofNumber || !m.MemberName || m.IdError) {
      if (m.KycStatus !== 'Pending' || m.ExtractedData) this.resetKyc(index);
      return;
    }

    m.KycStatus = 'Processing';
    this.cdr.detectChanges();

    this.kycService.verifyNewMemberKyc(m.IdProofType, m.IdProofNumber, m.MemberName, file).subscribe({
      next: (res) => {
        m.KycStatus = res.isSuccess ? 'Verified' : 'Failed';
        m.ExtractedData = res;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('KYC Error:', err);
        m.KycStatus = 'Failed';
        this.cdr.detectChanges();
      }
    });
  }

  resetKyc(index: number) {
    const m = this.members[index];
    // If it's already reset, don't clear the IdError which might have been set by validateIdField
    if (m.KycStatus === 'Pending' && !m.ExtractedData) return;
    
    m.KycStatus = 'Pending';
    m.ExtractedData = null;
    m.IdError = ''; 
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  validateIdField(index: number) {
    const m = this.members[index];
    this.resetKyc(index); 
    
    if (!m.IdProofType || !m.IdProofNumber) {
      m.IdError = '';
      return;
    }

    const idType = m.IdProofType.toUpperCase().replace(/\s/g, ''); 
    const pattern = KycService.ID_PATTERNS[idType];
    const sanitizedId = m.IdProofNumber.replace(/\s/g, '');
    
    if (pattern && !pattern.test(sanitizedId)) {
      m.IdError = `Invalid ${m.IdProofType} format.`;
    } else {
      m.IdError = '';
    }
    this.cdr.detectChanges();
  }

  printProof() {
    if (!this.draftId) {
      alert('Please save your progress before printing.');
      return;
    }

    this.policyService.downloadPolicyApplication(this.draftId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `PolicyApplication_${this.draftId}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Error downloading application proof:', err);
        alert('Could not generate application proof. Please ensure your draft is saved.');
      }
    });
  }

  submit() {
    this.processing = true;
    const fd = new FormData();

    // The backend SubmitDraft endpoint uses these specific keys
    const policyJson = {
      planId: this.planId,
      startDate: this.policyData.startDate,
      premiumFrequency: this.policyData.premiumFrequency,
      termYears: this.policyData.termYears
    };

    fd.append('policy', JSON.stringify(policyJson));
    fd.append('members', JSON.stringify(this.members));
    fd.append('nominees', JSON.stringify(this.nominees));

    // Match the IFormFile property names in CreatePolicyDto
    if (this.identityProof) fd.append('CustomerDocuments', this.identityProof);
    if (this.incomeProof) fd.append('CustomerDocuments', this.incomeProof);

    this.memberDocuments.forEach((f) => {
      if (f) fd.append('MemberDocuments', f);
    });

    // Choose the endpoint based on whether we have a draftId
    const request = this.draftId
      ? this.policyService.submitDraft(this.draftId, fd)
      : this.policyService.buyPolicy(fd);

    request.subscribe({
      next: (res) => {
        console.log('Success:', res);
        // Clean up local draft state if necessary
        this.draftId = null;
        this.router.navigate(['/customer-dashboard/my-policies']);
      },
      error: (err) => {
        this.processing = false;
        console.error('Submission Error:', err.error);
        const errorMessage = err.error?.message || err.error || 'Submission failed';
        alert(`Error: ${errorMessage}`);
      }
    });
  }

  calculateTotalPremium(): number {
    if (!this.selectedPlan || !this.members.length) return 0;

    let total = 0;
    const frequency = this.policyData.premiumFrequency;
    const term = this.policyData.termYears;

    this.members.forEach(m => {
      // 1. Annual Base Premium: (Coverage / 1000) * BaseRate
      let annualPremium = (m.CoverageAmount / 1000) * this.selectedPlan.baseRate;

      // 2. Age Factor
      const age = this.getAge(m.DateOfBirth);
      let ageFactor = 2.2;
      if (age <= 25) ageFactor = 0.8;
      else if (age <= 35) ageFactor = 1.0;
      else if (age <= 45) ageFactor = 1.3;
      else if (age <= 55) ageFactor = 1.7;

      // 3. Smoker & Gender Factors
      const smokerFactor = m.IsSmoker ? 1.5 : 1.0;
      const genderFactor = m.Gender?.toLowerCase() === 'female' ? 0.9 : 1.0;

      // 4. Term Factor
      let termFactor = 1.3;
      if (this.isWholeLifePlan) termFactor = 1.5; // Lifelong coverage premium factor
      else if (term <= 10) termFactor = 1.0;
      else if (term <= 20) termFactor = 1.1;
      else if (term <= 30) termFactor = 1.2;

      // 5. Final Annual Calculation with 18% GST (1.18m)
      const withGst = annualPremium * ageFactor * smokerFactor * genderFactor * termFactor * 1.18;

      // 6. Split by Frequency
      let finalMemberPremium = withGst;
      if (frequency === 'Monthly') finalMemberPremium = withGst / 12;
      else if (frequency === 'Quarterly') finalMemberPremium = withGst / 4;

      total += finalMemberPremium;
    });

    return Math.round(total * 100) / 100; // Round to 2 decimals
  }

  isSubmitted = false;

  nextStep() {
    this.isSubmitted = true;
    if (this.isFormValid()) {
      this.saveOrUpdateDraft();
      this.step++;
      this.isSubmitted = false; // Reset for the next step
      window.scrollTo(0, 0);
    }
  }

  prevStep() { this.step--; window.scrollTo(0, 0); }
}