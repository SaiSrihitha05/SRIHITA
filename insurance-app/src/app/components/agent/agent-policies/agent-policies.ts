import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../services/policy-service';

@Component({
  selector: 'app-agent-policies',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agent-policies.html'
})
export class AgentPolicies implements OnInit {
  private policyService = inject(PolicyService);
  private cdr = inject(ChangeDetectorRef);

  policies: any[] = [];
  loading = true;
  selectedPolicy: any = null; // For viewing deep details like members/documents
  showDetailsModal = false;

  statusOptions = ['Pending', 'Active', 'Expired', 'Cancelled', 'Rejected', 'Matured', 'Closed'];

  ngOnInit() { this.loadMyPolicies(); }

  loadMyPolicies() {
    this.policyService.getAgentPolicies().subscribe(data => {
      this.policies = data;
      this.loading = false;
      this.cdr.detectChanges();
    });
  }

  onUpdateStatus(policy: any) {
    // Collect remarks from the agent
    const remarks = window.prompt(`Please provide a reason for changing the status of policy ${policy.policyNumber}:`, 'Verification check complete');

    if (remarks === null) {
      // User cancelled the prompt, revert the status change
      this.loadMyPolicies();
      return;
    }

    const dto = {
      status: policy.status,
      remarks: remarks || 'Status updated by Agent'
    };

    this.policyService.updatePolicyStatus(policy.id, dto).subscribe({
      next: () => {
        // Status is already updated in UI via ngModel
        this.cdr.detectChanges();
      },
      error: () => {
        alert('Status update failed');
        this.loadMyPolicies(); // Revert on error
      }
    });
  }

  viewPolicyDetails(policy: any) {
    this.selectedPolicy = policy;
    this.showDetailsModal = true;
  }
}