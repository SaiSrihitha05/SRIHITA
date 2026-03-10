import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PolicyService } from '../../../services/policy-service';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-my-policies',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './my-policies.html'
})
export class MyPolicies implements OnInit {
  private policyService = inject(PolicyService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  policies: any[] = [];
  loading = true;
  selectedStatus: string = 'All';
  uniqueStatuses: string[] = [];

  ngOnInit() {
    this.loadPolicies();
  }

  loadPolicies() {
    this.policyService.getMyPolicies().subscribe({
      next: (data) => {
        this.policies = data;
        // Get unique status values from the fetched policies
        this.uniqueStatuses = ['All', ...new Set(data.map((p: any) => p.status))];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching policies', err);
        this.loading = false;
      }
    });
  }
  get filteredPolicies() {
    if (this.selectedStatus === 'All') return this.policies;
    return this.policies.filter(p => p.status === this.selectedStatus);
  }

  // Logic to determine if a premium is due or in grace period
  getPremiumStatus(policy: any) {
    if (policy.status !== 'Active') return null;

    const today = new Date();
    const dueDate = new Date(policy.nextDueDate);
    const graceEndDate = new Date(dueDate);

    // Defaulting to 30 days if gracePeriodDays isn't in the response, 
    // though ideally it comes from the Plan object inside the policy
    const graceDays = policy.gracePeriodDays || 30;
    graceEndDate.setDate(dueDate.getDate() + graceDays);

    if (today >= dueDate && today <= graceEndDate) {
      return { label: 'In Grace Period', class: 'text-orange-600 dark:text-orange-400 bg-orange-50 dark:bg-orange-900/20 border-orange-100 dark:border-orange-800' };
    } else if (today > graceEndDate) {
      return { label: 'Payment Overdue', class: 'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 border-red-100 dark:border-red-800' };
    } else if (this.isNear(dueDate, 7)) {
      return { label: 'Due Soon', class: 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 border-blue-100 dark:border-blue-800' };
    }
    return null;
  }

  // Add these helper methods to your existing MyPolicies class

  // 1. Check if the payment button should be visible
  canPayPremium(policy: any): boolean {
    return policy.status === 'Active';
  }
  getTotalSumAssured(policy: any): number {
    if (!policy || !policy.members) return 0;
    return policy.members.reduce((acc: number, member: any) => acc + member.coverageAmount, 0);
  }

  // 2. Handle the Details click
  viewPolicyDetails(policyId: number) {
    // Navigates to the route we defined: /customer-dashboard/policy-details/123
    this.router.navigate(['/customer-dashboard/policy-details', policyId]);
  }

  // 3. Handle the Pay Now click
  processPayment(policy: any) {
    // Navigate to a payment gateway or premium payment page
    this.router.navigate(['/customer-dashboard/pay-premium'], {
      queryParams: { policyId: policy.id, amount: policy.totalPremiumAmount }
    });
  }

  private isNear(date: Date, days: number): boolean {
    const today = new Date();
    const diff = date.getTime() - today.getTime();
    return diff > 0 && diff < (days * 24 * 60 * 60 * 1000);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Active': return 'bg-green-500 text-white';
      case 'Pending': return 'bg-amber-400 text-white';
      case 'Lapsed': return 'bg-red-500 text-white';
      case 'Cancelled': return 'bg-red-400 text-white';
      case 'Expired': return 'bg-gray-500 text-white';
      case 'Matured': return 'bg-blue-500 text-white';
      case 'Closed': return 'bg-purple-500 text-white';

      default: return 'bg-blue-500 text-white';
    }
  }
}