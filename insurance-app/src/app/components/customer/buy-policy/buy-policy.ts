import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PolicyService } from '../../../services/policy-service';
import { PlanService } from '../../../services/plan-service';

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
  private cdr = inject(ChangeDetectorRef);

  step = 1;
  planId: number = 0;
  selectedPlan: any = null;
  draftId: number | null = null;
  isSavingDraft = false;
  processing: boolean = false;

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

  // Files for [FromForm]
  identityProof: File | null = null;
  incomeProof: File | null = null;
  memberDocuments: File[] = [];

  ngOnInit() {
    const planIdParam = this.route.snapshot.queryParams['planId'];
    const draftIdParam = this.route.snapshot.queryParams['draftId'];

    if (draftIdParam) {
      this.loadDraft(Number(draftIdParam));
    } else if (planIdParam) {
      this.planId = Number(planIdParam);
      this.loadPlan(this.planId);
    }
  }

  loadPlan(planId: number) {
    this.planService.getPlanById(planId).subscribe(plan => {
      this.selectedPlan = plan;
      if (!this.policyData.termYears) {
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

        // Restore policy data
        this.policyData.startDate = draft.startDate
          ? new Date(draft.startDate).toISOString().split('T')[0]
          : '';
        this.policyData.termYears = draft.termYears || this.selectedPlan.minTermYears;
        this.policyData.premiumFrequency = draft.premiumFrequency || 'Monthly';

        // Restore members
        if (draft.members && draft.members.length > 0) {
          this.numMembers = draft.members.length;
          this.members = draft.members.map((m: any) => ({
            MemberName: m.memberName,
            RelationshipToCustomer: m.relationshipToCustomer,
            DateOfBirth: m.dateOfBirth
              ? new Date(m.dateOfBirth).toISOString().split('T')[0]
              : '',
            Gender: m.gender,
            CoverageAmount: m.coverageAmount,
            IsSmoker: m.isSmoker,
            HasPreExistingDiseases: m.hasPreExistingDiseases,
            DiseaseDescription: m.diseaseDescription || '',
            Occupation: m.occupation,
            IsPrimaryInsured: m.isPrimaryInsured
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
          Occupation: '', IsPrimaryInsured: i === 0
        });
      }
    } else { this.members.splice(this.numMembers); }
    this.cdr.detectChanges();
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
        DateOfBirth: m.DateOfBirth || null
      })),
      nominees: this.nominees.filter(n => n.NomineeName).map(n => ({
        ...n,
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
    const isTermValid = this.policyData.termYears >= this.selectedPlan.minTermYears &&
      this.policyData.termYears <= this.selectedPlan.maxTermYears;

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

        return hasBasicInfo && hasValidAge && hasValidCoverage && hasDocument;
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
    if (type === 'id') this.identityProof = file;
    if (type === 'income') this.incomeProof = file;
    if (type === 'member' && index !== undefined) this.memberDocuments[index] = file;
    this.cdr.detectChanges();
  }

  printProof() { window.print(); }

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
  // Add this to your BuyPolicy class

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
      if (term <= 10) termFactor = 1.0;
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