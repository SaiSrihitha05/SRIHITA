import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PlanService } from '../../../services/plan-service';
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
  private cdr = inject(ChangeDetectorRef);

  plans: any[] = [];
  selectedPlan: any = null;
  showDetailsModal = false;

  // Comparison state
  compareList: any[] = [];
  showCompareModal = false;

  filters: any = {
    planType: '',
    age: null,
    coverageAmount: null,
    termYears: null,
    hasMaturityBenefit: null,
    isReturnOfPremium: null,
    hasDeathBenefit: null,
    hasBonus: null,
    hasLoanFacility: null,
    coverageIncreasing: null,
    maxLoanInterestRate: null,
    minMaxLoanPercentage: null,
    maxLoanEligibleAfterYears: null,
    minCoverageUntilAge: null,
    minCoverageIncreaseRate: null
  };

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
    // If a specific category filter is applied, only show that category
    const categories = this.filters.planType
      ? [this.filters.planType]
      : ['TermLife', 'Endowment', 'Savings', 'WholeLife', 'Others'];

    return categories.map(cat => ({
      key: cat,
      title: this.categoryMetadata[cat]?.title || cat,
      description: this.categoryMetadata[cat]?.description || 'Explore our range of ' + cat + ' options.',
      plans: this.plans.filter(p => p.planType === cat)
    })).filter(group => group.plans.length > 0);
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
    const hasFilter = Object.values(this.filters).some(val => val !== null && val !== '');

    if (!hasFilter) {
      this.loadPlans();   // no filters = load all
      return;
    }

    this.planService.getFilteredPlans(this.filters).subscribe({
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
  }
  resetFilters() {
    this.filters = {
      planType: '',
      age: null,
      coverageAmount: null,
      termYears: null,
      hasMaturityBenefit: null,
      isReturnOfPremium: null,
      hasDeathBenefit: null,
      hasBonus: null,
      hasLoanFacility: null,
      coverageIncreasing: null,
      maxLoanInterestRate: null,
      minMaxLoanPercentage: null,
      maxLoanEligibleAfterYears: null,
      minCoverageUntilAge: null,
      minCoverageIncreaseRate: null
    };
    this.loadPlans();
  }
  toggleCompare(plan: any) {
    const index = this.compareList.findIndex(p => p.id === plan.id);
    if (index > -1) {
      this.compareList.splice(index, 1);
    } else if (this.compareList.length < 3) {
      this.compareList.push(plan);
    } else {
      alert("Selection limit reached: Compare up to 3 plans only.");
    }
    this.cdr.detectChanges();
  }

  isComparing(plan: any): boolean {
    return this.compareList.some(p => p.id === plan.id);
  }

  viewDetails(plan: any) {
    this.selectedPlan = plan;
    this.showDetailsModal = true;
    this.cdr.detectChanges();
  }

  closeDetails() {
    this.selectedPlan = null;
    this.showDetailsModal = false;
    this.cdr.detectChanges();
  }

  getBenefitList(plan: any) {
    const list = [];
    if (plan.hasMaturityBenefit) list.push({ label: 'Maturity Benefit', desc: 'Lump sum payout at end of term', available: true });
    if (plan.isReturnOfPremium) list.push({ label: 'Return of Premium', desc: 'All paid premiums returned if no claim', available: true });
    if (plan.hasDeathBenefit) list.push({ label: 'Death Benefit', desc: 'Financial protection for family', available: true });
    if (plan.hasBonus) list.push({ label: 'Bonus & Profits', desc: 'Reversionary bonuses added annually', available: true });
    if (plan.hasLoanFacility) list.push({ label: 'Loan Facility', desc: `Eligible after ${plan.loanEligibleAfterYears} years. Borrow up to ${plan.maxLoanPercentage}% of surrender value at ${plan.loanInterestRate}% interest.`, available: true });
    if (plan.coverageIncreasing) list.push({ label: 'Dynamic Coverage', desc: `Automatic ${plan.coverageIncreaseRate}% annual sum assured growth until age ${plan.coverageUntilAge}.`, available: true });

    list.push({ label: 'Nominee Coverage', available: true, desc: `Up to ${plan.maxNominees} nominees can be assigned for legal protection.` });

    return list;
  }
}