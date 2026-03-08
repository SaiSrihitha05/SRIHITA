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

  // Filter state
filters = { 
  planType: '', 
  age: null, 
  coverageAmount: null, 
  termYears: null, 
  hasMaturityBenefit: null, 
  isReturnOfPremium: null 
};

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
  const hasFilter = this.filters.planType || 
                    this.filters.age != null || 
                    this.filters.coverageAmount != null ||
                    this.filters.termYears != null;

  if (!hasFilter) {
    this.loadPlans();   // no filters = load all
    return;
  }

  this.planService.getFilteredPlans(this.filters).subscribe({
    next: (data) => {
      this.plans = data;
      this.cdr.detectChanges();
    },
    error: (err) => {
      alert(err.error?.detail || "No plans found matching these criteria.");
      this.loadPlans();
    }
  });
}
resetFilters() {
  this.filters = { 
    planType: '', age: null, coverageAmount: null, 
    termYears: null, hasMaturityBenefit: null, isReturnOfPremium: null 
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
    return [
      { label: 'Maturity Benefit', available: plan.hasMaturityBenefit, desc: 'Payout provided at the end of the policy term.' },
      { label: 'Return of Premium', available: plan.isReturnOfPremium, desc: 'Refund of base premiums if no claims are filed.' },
      { label: 'Nominee Coverage', available: true, desc: `Up to ${plan.maxNominees} nominees can be assigned for legal protection.` }
    ];
  }
}