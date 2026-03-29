import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-claims-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class ClaimsOfficerDashboard implements OnInit {
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

  stats: any = null;
  loading = true;

  ngOnInit() {
    this.fetchDashboardStats();
  }

  fetchDashboardStats() {
    this.http.get('https://localhost:7027/api/Dashboard/claims-officer').subscribe({
      next: (data) => {
        this.stats = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching dashboard stats', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }
}