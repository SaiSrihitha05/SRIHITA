import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { PlanService } from '../../../services/plan-service';
import { BenefitGlossaryService } from '../../../services/benefit-glossary.service';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-explore-plans',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './explore-plans.html'
})
export class ExplorePlans implements OnInit {
  private planService = inject(PlanService);
  public glossary = inject(BenefitGlossaryService);
  private http = inject(HttpClient);
  public cdr = inject(ChangeDetectorRef);
  private filterTimeout: any;

  plans: any[] = [];
  selectedPlan: any = null;
  showDetailsModal = false;

  // Comparison state
  selectedForCompare: number[] = [];
  comparisonResult: any = null;
  isComparingPlans = false;
  filters: any = {
    planType: '',
    age: 0,
    coverageAmount: 0,
    termYears: 0,
    hasMaturityBenefit: false,
    isReturnOfPremium: false,
    hasDeathBenefit: false,
    hasBonus: false,
    hasLoanFacility: false,
    coverageIncreasing: false,
    maxLoanInterestRate: 0,
    minMaxLoanPercentage: 0,
    maxLoanEligibleAfterYears: 0,
    minCoverageUntilAge: 0,
    minCoverageIncreaseRate: 0,
    maxNominees: 0,
    maxPolicyMembersAllowed: 0
  };

  sortBy: string = 'default';

  // For filter chips display
  get activeFilterChips() {
    const chips = [];
    if (this.filters.planType) chips.push({ label: `Category: ${this.filters.planType}`, field: 'planType' });
    if (this.filters.age > 0) chips.push({ label: `Age: ${this.filters.age}`, field: 'age' });
    if (this.filters.coverageAmount > 0) chips.push({ label: `Coverage: ₹${(this.filters.coverageAmount / 100000).toFixed(1)}L`, field: 'coverageAmount' });
    if (this.filters.termYears > 0) chips.push({ label: `Term: ${this.filters.termYears}y`, field: 'termYears' });
    if (this.filters.maxNominees > 0) {
      chips.push({ label: `Max Nominees: ${this.filters.maxNominees}`, field: 'nominees' });
    }
    if (this.filters.maxPolicyMembersAllowed > 0) {
      chips.push({ label: `Max Members: ${this.filters.maxPolicyMembersAllowed}`, field: 'members' });
    }

    // Toggles
    if (this.filters.hasMaturityBenefit) chips.push({ label: 'Maturity Benefit', field: 'hasMaturityBenefit' });
    if (this.filters.isReturnOfPremium) chips.push({ label: 'Return Premium', field: 'isReturnOfPremium' });
    if (this.filters.hasBonus) chips.push({ label: 'Bonus & Profits', field: 'hasBonus' });
    if (this.filters.hasLoanFacility) chips.push({ label: 'Loan Facility', field: 'hasLoanFacility' });
    if (this.filters.coverageIncreasing) chips.push({ label: 'Increasing Cover', field: 'coverageIncreasing' });

    return chips;
  }

  categoryMetadata: any = {
    'TermLife': {
      title: 'Term Life Plans',
      description: 'Pure protection for your family. Get high coverage at affordable premiums to secure your loved ones\' future.'
    },
    'Endowment': {
      title: 'Endowment Plans',
      description: 'A perfect blend of protection and savings. Get life cover plus a guaranteed lump sum payout on maturity.'
    },
    'Savings': {
      title: 'Savings Plans',
      description: 'Guaranteed returns to meet your life goals. Build a corpus for education, marriage, or wealth creation.'
    },
    'WholeLife': {
      title: 'Whole Life Plans',
      description: 'Lifelong protection until age 100. Ensure a legacy for your family with permanent life insurance coverage.'
    },
    'Others': {
      title: 'Other Specialized Plans',
      description: 'Exclusive insurance solutions tailored for unique needs and specific lifestyle requirements.'
    }
  };

  get groupedPlans() {
    const categories = this.filters.planType
      ? [this.filters.planType]
      : ['TermLife', 'Endowment', 'Savings', 'WholeLife', 'Others'];

    const groups = categories.map(cat => ({
      key: cat,
      title: this.categoryMetadata[cat]?.title || cat,
      description: this.categoryMetadata[cat]?.description || 'Explore our range of ' + cat + ' options.',
      plans: this.plans.filter(p => p.planType === cat)
    })).filter(group => group.plans.length > 0);

    // Apply sorting to each group's plans
    groups.forEach(group => {
      group.plans.sort((a, b) => {
        switch (this.sortBy) {
          case 'premium-low': return a.baseRate - b.baseRate; // Approximation
          case 'coverage-high': return b.maxCoverageAmount - a.maxCoverageAmount;
          case 'age-low': return a.minAge - b.minAge;
          default: return 0;
        }
      });
    });

    return groups;
  }

  ngOnInit() {
    this.loadPlans();
  }


  loadPlans() {
    this.planService.getPlans().subscribe(data => {
      this.plans = data;
      this.cdr.detectChanges();
    });
  }

  applyFilter() {
    if (this.filterTimeout) clearTimeout(this.filterTimeout);

    this.filterTimeout = setTimeout(() => {
      // Check if any filter is actually active (non-null, non-empty, and NOT false for booleans)
      const activeFilters = Object.values(this.filters).filter(val =>
        val !== null && val !== '' && val !== false && val !== 0
      );

      if (activeFilters.length === 0) {
        this.loadPlans();
        return;
      }

      // Create a copy and strip out 0 values for the API call
      const queryFilters = { ...this.filters };
      Object.keys(queryFilters).forEach(key => {
        if (queryFilters[key] === 0 || queryFilters[key] === null || queryFilters[key] === '') {
          delete queryFilters[key];
        }
      });

      this.planService.getFilteredPlans(queryFilters).subscribe({
        next: (data) => {
          this.plans = data || [];
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("Filter error:", err);
          this.plans = [];
          this.cdr.detectChanges();
        }
      });
    }, 300); // 300ms debounce
  }
  resetFilters() {
    this.filters = {
      planType: '',
      age: 0,
      coverageAmount: 0,
      termYears: 0,
      hasMaturityBenefit: false,
      isReturnOfPremium: false,
      hasDeathBenefit: false,
      hasBonus: false,
      hasLoanFacility: false,
      coverageIncreasing: false,
      maxLoanInterestRate: 0,
      minMaxLoanPercentage: 0,
      maxLoanEligibleAfterYears: 0,
      minCoverageUntilAge: 0,
      minCoverageIncreaseRate: 0,
      maxNominees: 0,
      maxPolicyMembersAllowed: 0
    };
    this.sortBy = 'default';

    // 2. Clear any pending debounced filter calls
    if (this.filterTimeout) clearTimeout(this.filterTimeout);

    // 3. Directly load all plans from the service
    this.planService.getPlans().subscribe({
      next: (data) => {
        this.plans = data;
        // 4. Force UI refresh
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  removeFilter(field: string) {
    if (field === 'nominees') {
      this.filters.maxNominees = 0;
    } else if (field === 'members') {
      this.filters.maxPolicyMembersAllowed = 0;
    } else {
      this.filters[field] = (typeof this.filters[field] === 'string') ? '' : 0;
    }
    this.applyFilter();
  }

  toggleCompare(planId: number) {
    if (this.selectedForCompare.includes(planId)) {
      this.selectedForCompare = this.selectedForCompare
        .filter(id => id !== planId);
    } else if (this.selectedForCompare.length < 3) {
      this.selectedForCompare.push(planId);
    } else {
      alert("Selection limit reached: Compare up to 3 plans only.");
    }
    this.comparisonResult = null; // Reset summary on change
    this.cdr.detectChanges();
  }

  comparePlans() {
    if (this.selectedForCompare.length < 2) return;
    this.isComparingPlans = true;
    this.comparisonResult = null;

    this.http.post<any>('https://localhost:7027/api/Plans/compare', {
      planIds: this.selectedForCompare
    }).subscribe({
      next: (result: any) => {
        this.comparisonResult = result;
        this.isComparingPlans = false;
        this.cdr.detectChanges();
        // Scroll to result
        setTimeout(() => {
          document.getElementById('comparison-panel')?.scrollIntoView({ behavior: 'smooth' });
        }, 100);
      },
      error: (err: any) => {
        this.isComparingPlans = false;
        alert("Failed to compare plans. Please try again later.");
        this.cdr.detectChanges();
      }
    });
  }

  isPlanSelected(planId: number): boolean {
    return this.selectedForCompare.includes(planId);
  }

  viewDetails(plan: any) {
    this.selectedPlan = plan;
    this.showDetailsModal = true;
    this.cdr.detectChanges();
  }

  closeDetails() {
    this.showDetailsModal = false;
    this.selectedPlan = null;
    this.cdr.detectChanges();
  }

  updateStepper(field: 'maxNominees' | 'maxPolicyMembersAllowed', change: number) {
    if (this.filters[field] === 0) {
      this.filters[field] = (change > 0) ? 1 : 0;
      this.applyFilter();
      return;
    }

    const newValue = this.filters[field] + change;

    if (newValue >= 1 && newValue <= 6) {
      this.filters[field] = newValue;
      this.applyFilter();
    }
  }

  getBenefitList(plan: any) {
    const list = [];
    if (plan.hasMaturityBenefit) list.push({ label: 'Maturity Benefit', desc: 'Guaranteed lump sum at end of term', available: true });
    if (plan.isReturnOfPremium) list.push({ label: 'Return of Premium', desc: 'Total premium back if no claim', available: true });
    if (plan.hasDeathBenefit) list.push({ label: 'Death Benefit', desc: 'Secure payout for beneficiaries', available: true });
    if (plan.hasBonus) list.push({ label: 'Bonus & Profits', desc: 'Annual profit-sharing additions', available: true });
    if (plan.hasLoanFacility) list.push({ label: 'Loan Facility', desc: `Eligible after ${plan.loanEligibleAfterYears} years. Borrow up to ${plan.maxLoanPercentage}% of surrender value at ${plan.loanInterestRate}% interest.`, available: true });
    if (plan.coverageIncreasing) list.push({ label: 'Dynamic Coverage', desc: `Automatic ${plan.coverageIncreaseRate}% annual sum assured growth until retirement.`, available: true });

    if (plan.reinstatementDays > 0) {
      list.push({ 
        label: 'Reinstatement', 
        desc: `Policy can be restored within ${plan.reinstatementDays} days of lapse by paying ₹${plan.reinstatementPenaltyAmount} penalty plus missed premiums.`, 
        available: true 
      });
    }

    list.push({ label: 'Nominee Coverage', available: true, desc: `Up to ${plan.maxNominees} nominees can be assigned for legal protection.` });

    return list;
  }
}