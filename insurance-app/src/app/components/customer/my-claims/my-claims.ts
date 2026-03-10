import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClaimService } from '../../../services/claim-service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-my-claims',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './my-claims.html'
})
export class MyClaims implements OnInit {
  private claimService = inject(ClaimService);
  private cdr = inject(ChangeDetectorRef);

  claims: any[] = [];
  loading = true;

  ngOnInit() {
    this.loadClaims();
  }

  loadClaims() {
    this.loading = true;
    this.claimService.getMyClaims().subscribe({
      next: (data) => {
        this.claims = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching claims:', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Submitted':   return 'bg-blue-500';
      case 'UnderReview': return 'bg-amber-500';
      case 'Settled':     return 'bg-green-500';
      case 'Rejected':    return 'bg-red-500';
      default:            return 'bg-gray-500';
    }
  }
}