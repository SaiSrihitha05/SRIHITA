import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { PolicyService } from '../../../services/policy-service';

@Component({
  selector: 'app-policy-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './policy-details.html'
})
export class PolicyDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private policyService = inject(PolicyService);
  private cdr = inject(ChangeDetectorRef);
  private router=inject(Router);

  policy: any = null;
  loading = true;

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.policyService.getPolicyById(id).subscribe({
      next: (data) => {
        this.policy = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  downloadDocument(docId: number, fileName: string) {
    this.policyService.downloadFile(docId).subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.click();
      window.URL.revokeObjectURL(url);
      this.cdr.detectChanges();
    });
  }
  onCancelPolicy() {
  if (confirm('Are you sure you want to cancel this pending application?')) {
    this.policyService.cancelPolicy(this.policy.id).subscribe({
      next: (res) => {
        alert(res.message);
        this.router.navigate(['/customer-dashboard/my-policies']);
      },
      error: (err) => {
        const msg = err.error?.message || "Failed to cancel policy";
        alert(msg);
      }
    });
  }
}
}