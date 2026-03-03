import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClaimService } from '../../../services/claim-service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-admin-claims',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-claims.html'
})
export class AdminClaims implements OnInit {
  private claimService = inject(ClaimService);
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

  claims: any[] = [];
  officers: any[] = [];
  loading = true;

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.claimService.getAllClaims().subscribe(data => {
      this.claims = data;
      this.loading = false;
      this.cdr.detectChanges();
    });

    // Fetch Claims Officers list for the dropdown
    this.http.get<any[]>('https://localhost:7027/api/Users/claims-officers').subscribe(data => {
      this.officers = data;
      this.cdr.detectChanges();
    });
  }

  onAssignOfficer(claimId: number, event: any) {
    const officerId = event.target.value;
    if (officerId) {
      this.claimService.assignClaimsOfficer(claimId, +officerId).subscribe({
        next: () => {
          alert('Claims Officer assigned successfully!');
          this.loadData();
          this.cdr.detectChanges();
        }
      });
    }
  }
}