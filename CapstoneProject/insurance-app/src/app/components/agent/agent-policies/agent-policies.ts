import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../services/policy-service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-agent-policies',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agent-policies.html'
})
export class AgentPolicies implements OnInit {
  private policyService = inject(PolicyService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);

  policies: any[] = [];
  loading = true;
  selectedPolicy: any = null;
  showDetailsModal = false;
  agentRemarks: string = '';
  processingAction: 'Approve' | 'Reject' | null = null;
  highlightedPolicyId: number | null = null;

  statusOptions = ['Pending', 'Active', 'Expired', 'Cancelled', 'Rejected', 'Matured', 'Closed'];

  ngOnInit() { 
    this.loadMyPolicies(); 
    
    const id = this.route.snapshot.queryParamMap.get('policyId');
    if (id) {
        this.highlightedPolicyId = parseInt(id);
        // Wait for policies to load before scrolling
    }
  }

  scrollToHighlighted() {
    if (!this.highlightedPolicyId) return;
    const el = document.getElementById(`policy-row-${this.highlightedPolicyId}`);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  loadMyPolicies() {
    this.policyService.getAgentPolicies().subscribe(data => {
      this.policies = data;
      this.loading = false;
      this.cdr.detectChanges();

      if (this.highlightedPolicyId) {
        setTimeout(() => this.scrollToHighlighted(), 500);
      }
    });
  }

  onUpdateStatus(policy: any) {
    const remarks = window.prompt(`Please provide a reason for changing the status of policy ${policy.policyNumber}:`, 'Verification check complete');

    if (remarks === null) {
      this.loadMyPolicies();
      return;
    }

    const dto = {
      status: policy.status,
      remarks: remarks || 'Status updated by Agent'
    };

    this.policyService.updatePolicyStatus(policy.id, dto).subscribe({
      next: () => {
        this.cdr.detectChanges();
      },
      error: () => {
        alert('Status update failed');
        this.loadMyPolicies();
      }
    });
  }

  viewPolicyDetails(policy: any) {
    this.selectedPolicy = policy;
    this.agentRemarks = '';
    this.showDetailsModal = true;
  }

  approvePolicy() {
    this.updateStatus('Active', 'Approve');
  }

  rejectPolicy() {
    this.updateStatus('Rejected', 'Reject');
  }

  private updateStatus(status: string, actionName: 'Approve' | 'Reject') {
    if (!this.agentRemarks) {
      alert('Please enter remarks before making a decision.');
      return;
    }

    this.processingAction = actionName;
    this.cdr.detectChanges(); // Force immediate update for the spinner

    const dto = {
      status: status,
      remarks: this.agentRemarks
    };

    this.policyService.updatePolicyStatus(this.selectedPolicy.id, dto).subscribe({
      next: () => {
        this.selectedPolicy.status = status;
        this.showDetailsModal = false;
        this.processingAction = null;
        this.loadMyPolicies();
        this.cdr.detectChanges();
      },
      error: () => {
        this.processingAction = null;
        this.cdr.detectChanges();
        alert('Status update failed');
      }
    });
  }

  isEligibleCheck(p: any) {
    if (!p) return null;

    const ages = p.members.map((m: any) => this.getAge(m.dateOfBirth));
    const isAgeValid = ages.every((a: number) => a >= p.planMinAge && a <= p.planMaxAge);

    const coverages = p.members.map((m: any) => m.coverageAmount);
    const isCoverageValid = coverages.every((c: number) => c >= p.planMinCoverageAmount && c <= p.planMaxCoverageAmount);

    const isMembersValid = p.members.length >= 1 && p.members.length <= p.planMaxMembers;

    const totalShare = p.nominees.reduce((sum: number, n: any) => sum + n.sharePercentage, 0);
    const isNomineesValid = p.nominees.length >= p.planMinNominees && 
                           p.nominees.length <= p.planMaxNominees && 
                           Math.abs(totalShare - 100) < 0.01;

    return {
      age: isAgeValid,
      coverage: isCoverageValid,
      members: isMembersValid,
      nominees: isNomineesValid
    };
  }

  downloadDocument(docId: number) {
    this.policyService.downloadFile(docId).subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `document_${docId}`;
      link.click();
      window.URL.revokeObjectURL(url);
    });
  }

  getAge(dob: string): number {
    if (!dob) return 0;
    const birth = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    if (today < new Date(today.getFullYear(), birth.getMonth(), birth.getDate())) age--;
    return age;
  }
}