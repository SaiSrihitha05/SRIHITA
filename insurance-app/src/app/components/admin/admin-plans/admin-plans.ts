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
  isViewOnly = false; // New property for read-only view

  // Form Object
  // Initial state for a new plan
  currentPlan: any = {
    planName: '',
    planType: '',
    description: '',
    baseRate: 0.01,
    minAge: 18,
    maxAge: 70,
    minCoverageAmount: 100000,
    maxCoverageAmount: 10000000,
    minTermYears: 5,
    maxTermYears: 30,
    gracePeriodDays: 30,
    hasMaturityBenefit: false,
    isReturnOfPremium: false,
    maxPolicyMembersAllowed: 1,
    minNominees: 1,
    maxNominees: 5,
    commissionRate: 10,
    isActive: true
  };

  ngOnInit() { this.loadPlans(); }

  loadPlans() {
    this.planService.getPlans().subscribe(data => {
      // Basic filtering to ensure Admin doesn't see draft plans if any exist
      // The user mentioned "if the policy status is draft that should not be visible"
      // Assuming plans might also have a status or we apply it to policy list
      this.plans = data.filter(p => p.status !== 'Draft');
      this.cdr.detectChanges();
    });
  }

  openCreateModal() {
    this.isEditMode = false;
    this.isViewOnly = false;
    this.currentPlan = {
      id: 0,
      planName: '',
      planType: '',
      minAge: 18,
      isActive: true,
      minNominees: 1,
      maxNominees: 5
    };
    this.showModal = true;
  }

  openEditModal(plan: any) {
    this.isEditMode = true;
    this.isViewOnly = false;
    this.currentPlan = { ...plan }; // Create a copy
    this.showModal = true;
  }

  openViewModal(plan: any) {
    this.isEditMode = false;
    this.isViewOnly = true;
    this.currentPlan = { ...plan };
    this.showModal = true;
  }

  savePlan() {
    if (this.isEditMode) {
      this.planService.updatePlan(this.currentPlan.id, this.currentPlan).subscribe(() => {
        this.loadPlans();
        this.showModal = false;
        this.cdr.detectChanges();
      });
    } else {
      this.planService.createPlan(this.currentPlan).subscribe(() => {
        this.loadPlans();
        this.showModal = false;
        this.cdr.detectChanges();
      });
    }
  }

  onDelete(id: number) {
    if (confirm('Delete this plan permanently?')) {
      this.planService.deletePlan(id).subscribe(() => {
        this.loadPlans();
        this.cdr.detectChanges();
      });
    }
  }
}