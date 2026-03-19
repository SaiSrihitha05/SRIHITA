import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlanService } from '../../../services/plan-service';

@Component({
  selector: 'app-admin-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-plans.html'
})
export class AdminPlans implements OnInit {
  private planService = inject(PlanService);
  private cdr = inject(ChangeDetectorRef);

  plans: any[] = [];
  showModal = false;
  isEditMode = false;
  isViewOnly = false;

  // ─────────────────────────────────────────────────────────────────
  // touched: tracks which fields the user has blurred.
  // Errors are ONLY shown after a field is blurred,
  // OR after a failed submit attempt (touchAll called).
  // ─────────────────────────────────────────────────────────────────
  touched: Record<string, boolean> = {};

  // ─────────────────────────────────────────────────────────────────
  // defaultPlan: COMPLETE reset — every field the backend
  // CreatePlanDto expects must live here.
  //
  // ROOT BUG: old openCreateModal() set a partial object missing:
  //   baseRate, commissionRate, minCoverageAmount, maxCoverageAmount,
  //   minTermYears, maxTermYears, gracePeriodDays,
  //   maxPolicyMembersAllowed, description
  // → those came through as undefined → backend threw BadRequestException
  //   (MinAge >= MaxAge etc. comparisons crashed on undefined).
  // ─────────────────────────────────────────────────────────────────
  private defaultPlan() {
    return {
      id: 0,
      planName: '',
      planType: '',
      description: '',
      baseRate: null as number | null,
      minAge: 18,
      maxAge: 70,
      minCoverageAmount: null as number | null,
      maxCoverageAmount: null as number | null,
      minTermYears: null as number | null,
      maxTermYears: null as number | null,
      gracePeriodDays: 30,
      maxPolicyMembersAllowed: 1,
      minNominees: 1,
      maxNominees: 5,
      commissionRate: null as number | null,
      isActive: true,
      hasDeathBenefit: true,
      hasMaturityBenefit: false,
      isReturnOfPremium: false,
      hasBonus: false,
      bonusRate: 0,
      terminalBonusRate: 0,
      hasLoanFacility: false,
      loanEligibleAfterYears: null as number | null,
      maxLoanPercentage: null as number | null,
      loanInterestRate: null as number | null,
      coverageIncreasing: false,
      coverageIncreaseRate: null as number | null,
      isCoverageUntilAge: false,
      coverageUntilAge: null as number | null,
    };
  }

  currentPlan: any = this.defaultPlan();

  ngOnInit() { this.loadPlans(); }

  loadPlans() {
    this.planService.getPlans().subscribe({
      next: (data) => {
        // ROOT BUG: old code → data.filter(p => p.status !== 'Draft')
        // Plans have NO 'status' field — that's PolicyAssignment.
        // This filter silently dropped every plan → empty table always.
        this.plans = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load plans:', err)
    });
  }

  openCreateModal() {
    this.isEditMode = false;
    this.isViewOnly = false;
    this.currentPlan = this.defaultPlan(); // FIX: was incomplete partial object
    this.touched = {};
    this.showModal = true;
  }

  openEditModal(plan: any) {
    this.isEditMode = true;
    this.isViewOnly = false;
    this.currentPlan = { ...plan };
    this.touched = {};
    this.showModal = true;
  }

  openViewModal(plan: any) {
    this.isEditMode = false;
    this.isViewOnly = true;
    this.currentPlan = { ...plan };
    this.touched = {};
    this.showModal = true;
  }

  // Mark a single field as touched on blur → shows its error
  touch(field: string) {
    this.touched[field] = true;
  }

  // Touch a min/max pair together so cross-validation updates both
  touchPair(a: string, b: string) {
    this.touched[a] = true;
    this.touched[b] = true;
  }

  // Should this field's error message be visible?
  showErr(field: string): boolean {
    return !!this.touched[field];
  }

  // Mark ALL required (+ active conditional) fields at once — called on submit
  private touchAll() {
    const always = [
      'planName', 'planType', 'description', 'baseRate', 'commissionRate',
      'minAge', 'maxAge', 'minCoverageAmount', 'maxCoverageAmount',
      'minTermYears', 'maxTermYears', 'gracePeriodDays',
      'maxPolicyMembersAllowed', 'minNominees', 'maxNominees',
    ];
    const conditional: string[] = [];
    if (this.currentPlan.hasBonus)
      conditional.push('bonusRate', 'terminalBonusRate');
    if (this.currentPlan.hasLoanFacility)
      conditional.push('loanEligibleAfterYears', 'maxLoanPercentage', 'loanInterestRate');
    if (this.currentPlan.coverageIncreasing)
      conditional.push('coverageIncreaseRate');
    if (this.currentPlan.isCoverageUntilAge)
      conditional.push('coverageUntilAge');
    [...always, ...conditional].forEach(f => (this.touched[f] = true));
  }

  // ─────────────────────────────────────────────────────────────────
  // getError: returns an error string or '' (valid).
  // Conditional fields return '' when their toggle is off.
  // ─────────────────────────────────────────────────────────────────
  getError(field: string): string {
    const v = this.currentPlan[field];
    const n = (x: any): number | null =>
      x !== null && x !== '' && !isNaN(+x) ? +x : null;

    switch (field) {
      case 'planName':
        if (!v || !v.trim()) return 'Plan name is required';
        if (v.trim().length < 3) return 'Must be at least 3 characters';
        return '';

      case 'planType':
        if (!v) return 'Plan category is required';
        return '';

      case 'description':
        if (!v || !v.trim()) return 'Description is required';
        if (v.trim().length < 10) return 'Must be at least 10 characters';
        return '';

      case 'baseRate': {
        const num = n(v);
        if (num === null) return 'Base rate is required';
        if (num <= 0) return 'Must be greater than 0';
        if (num > 100) return 'Cannot exceed 100%';
        return '';
      }
      case 'commissionRate': {
        const num = n(v);
        if (num === null) return 'Commission rate is required';
        if (num < 0) return 'Cannot be negative';
        if (num > 50) return 'Cannot exceed 50%';
        return '';
      }
      case 'minAge': {
        const num = n(v); const max = n(this.currentPlan.maxAge);
        if (num === null) return 'Min age is required';
        if (num < 0) return 'Cannot be negative';
        if (max !== null && num >= max) return 'Must be less than max age';
        return '';
      }
      case 'maxAge': {
        const num = n(v); const min = n(this.currentPlan.minAge);
        if (num === null) return 'Max age is required';
        if (num > 100) return 'Cannot exceed 100';
        if (min !== null && num <= min) return 'Must be greater than min age';
        return '';
      }
      case 'minCoverageAmount': {
        const num = n(v); const max = n(this.currentPlan.maxCoverageAmount);
        if (num === null) return 'Min coverage is required';
        if (num <= 0) return 'Must be greater than 0';
        if (max !== null && num >= max) return 'Must be less than max coverage';
        return '';
      }
      case 'maxCoverageAmount': {
        const num = n(v); const min = n(this.currentPlan.minCoverageAmount);
        if (num === null) return 'Max coverage is required';
        if (num <= 0) return 'Must be greater than 0';
        if (min !== null && num <= min) return 'Must be greater than min coverage';
        return '';
      }
      case 'minTermYears': {
        if (this.currentPlan.isCoverageUntilAge) return '';
        const num = n(v); const max = n(this.currentPlan.maxTermYears);
        if (num === null) return 'Min term is required';
        if (num < 1) return 'Must be at least 1 year';
        if (max !== null && num >= max) return 'Must be less than max term';
        return '';
      }
      case 'maxTermYears': {
        if (this.currentPlan.isCoverageUntilAge) return '';
        const num = n(v); const min = n(this.currentPlan.minTermYears);
        if (num === null) return 'Max term is required';
        if (num < 1) return 'Must be at least 1 year';
        if (min !== null && num <= min) return 'Must be greater than min term';
        return '';
      }
      case 'gracePeriodDays': {
        const num = n(v);
        if (num === null) return 'Grace period is required';
        if (num < 0) return 'Cannot be negative';
        if (num > 365) return 'Cannot exceed 365 days';
        return '';
      }
      case 'maxPolicyMembersAllowed': {
        const num = n(v);
        if (num === null) return 'Max members is required';
        if (num < 1) return 'Must be at least 1';
        return '';
      }
      case 'minNominees': {
        const num = n(v); const max = n(this.currentPlan.maxNominees);
        if (num === null) return 'Min nominees is required';
        if (num < 1) return 'Must be at least 1';
        if (max !== null && num > max) return 'Must be ≤ max nominees';
        return '';
      }
      case 'maxNominees': {
        const num = n(v); const min = n(this.currentPlan.minNominees);
        if (num === null) return 'Max nominees is required';
        if (num < 1) return 'Must be at least 1';
        if (min !== null && num < min) return 'Must be ≥ min nominees';
        return '';
      }

      // ── Conditional: hasBonus ────────────────────────────────
      case 'bonusRate': {
        if (!this.currentPlan.hasBonus) return '';
        const num = n(v);
        if (num === null) return 'Bonus rate is required';
        if (num < 0) return 'Cannot be negative';
        return '';
      }
      case 'terminalBonusRate': {
        if (!this.currentPlan.hasBonus) return '';
        const num = n(v);
        if (num === null) return 'Terminal bonus rate is required';
        if (num < 0) return 'Cannot be negative';
        return '';
      }

      // ── Conditional: hasLoanFacility ─────────────────────────
      case 'loanEligibleAfterYears': {
        if (!this.currentPlan.hasLoanFacility) return '';
        const num = n(v);
        if (num === null) return 'Eligible years is required';
        if (num < 1) return 'Must be at least 1 year';
        return '';
      }
      case 'maxLoanPercentage': {
        if (!this.currentPlan.hasLoanFacility) return '';
        const num = n(v);
        if (num === null) return 'Max loan % is required';
        if (num <= 0) return 'Must be greater than 0';
        if (num > 100) return 'Cannot exceed 100%';
        return '';
      }
      case 'loanInterestRate': {
        if (!this.currentPlan.hasLoanFacility) return '';
        const num = n(v);
        if (num === null) return 'Interest rate is required';
        if (num <= 0) return 'Must be greater than 0';
        return '';
      }

      // ── Conditional: isCoverageUntilAge ─────────────────────
      case 'coverageUntilAge': {
        if (!this.currentPlan.isCoverageUntilAge) return '';
        const num = n(v);
        if (num === null) return 'Target age is required';
        if (num <= this.currentPlan.maxAge) return `Must be > max entry age (${this.currentPlan.maxAge})`;
        if (num > 100) return 'Cannot exceed age 100';
        return '';
      }

      default: return '';
    }
  }

  // True when every required + active-conditional field is valid
  get isFormValid(): boolean {
    const always = [
      'planName', 'planType', 'description', 'baseRate', 'commissionRate',
      'minAge', 'maxAge', 'minCoverageAmount', 'maxCoverageAmount',
      'gracePeriodDays', 'maxPolicyMembersAllowed', 'minNominees', 'maxNominees',
    ];
    if (!this.currentPlan.isCoverageUntilAge) {
      always.push('minTermYears', 'maxTermYears');
    }

    const conditional: string[] = [];
    if (this.currentPlan.hasBonus)
      conditional.push('bonusRate', 'terminalBonusRate');
    if (this.currentPlan.hasLoanFacility)
      conditional.push('loanEligibleAfterYears', 'maxLoanPercentage', 'loanInterestRate');
    if (this.currentPlan.coverageIncreasing)
      conditional.push('coverageIncreaseRate');
    if (this.currentPlan.isCoverageUntilAge)
      conditional.push('coverageUntilAge');
    return [...always, ...conditional].every(f => this.getError(f) === '');
  }

  savePlan() {
    this.touchAll();           // reveal all errors
    if (!this.isFormValid) return; // block if invalid

    // Zero-out fields that belong to disabled toggles
    const payload = { ...this.currentPlan };
    if (!payload.hasBonus) { payload.bonusRate = 0; payload.terminalBonusRate = 0; }
    if (!payload.hasLoanFacility) {
      payload.loanEligibleAfterYears = 0;
      payload.maxLoanPercentage = 0;
      payload.loanInterestRate = 0;
    }
    if (!payload.coverageIncreasing) payload.coverageIncreaseRate = 0;

    if (this.isEditMode) {
      this.planService.updatePlan(payload.id, payload).subscribe({
        next: () => { this.loadPlans(); this.showModal = false; this.cdr.detectChanges(); },
        error: (err) => console.error('Update failed:', err)
      });
    } else {
      this.planService.createPlan(payload).subscribe({
        next: () => { this.loadPlans(); this.showModal = false; this.cdr.detectChanges(); },
        error: (err) => console.error('Create failed:', err)
      });
    }
  }

  onDelete(id: number) {
    if (confirm('Delete this plan permanently?')) {
      this.planService.deletePlan(id).subscribe({
        next: () => { this.loadPlans(); this.cdr.detectChanges(); },
        error: (err) => console.error('Delete failed:', err)
      });
    }
  }
}