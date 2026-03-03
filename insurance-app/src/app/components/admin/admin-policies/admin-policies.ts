import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PolicyService } from '../../../services/policy-service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-admin-policies',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-policies.html'
})
export class AdminPolicies implements OnInit {
  private policyService = inject(PolicyService);
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

  policies: any[] = [];
  agents: any[] = [];
  loading = true;

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    // Parallel load policies and agent list
    this.policyService.getAllPolicies().subscribe(data => {
      // Filter out Draft policies as they are not yet submitted/ready for admin oversight
      this.policies = data.filter(p => p.status !== 'Draft');
      this.loading = false;
      this.cdr.detectChanges();
    });

    this.http.get<any[]>('https://localhost:7027/api/Users/agents').subscribe(data => {
      this.agents = data;
      this.cdr.detectChanges();
    });
  }

  onAssignAgent(policyId: number, event: any) {
    const agentId = event.target.value;
    if (agentId) {
      this.policyService.assignAgent(policyId, +agentId).subscribe({
        next: () => {
          alert('Agent assigned successfully!');
          this.loadData(); // Refresh list
          this.cdr.detectChanges();
        }
      });
    }
  }
}