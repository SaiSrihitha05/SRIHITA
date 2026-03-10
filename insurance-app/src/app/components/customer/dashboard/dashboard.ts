import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './dashboard.html'
})
export class Dashboard implements OnInit {
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

  stats: any = null;
  loading: boolean = true;

  // Chart Properties
  public claimsChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false // Using custom legend grid in HTML
      },
      tooltip: {
        backgroundColor: '#75013f',
        titleFont: { family: 'Lato', size: 12 },
        bodyFont: { family: 'Lato', size: 11 }
      }
    }
  };

  public claimsChartData: ChartData<'doughnut'> = {
    labels: [],
    datasets: [{
      data: [],
      backgroundColor: ['#75013f', '#fe3082', '#3b82f6', '#fb923c', '#10b981'],
      borderWidth: 0,
      hoverOffset: 10
    }]
  };
  public claimsChartType: ChartType = 'doughnut';

  ngOnInit() {
    this.fetchCustomerDashboard();
  }

  fetchCustomerDashboard() {
    this.http.get('https://localhost:7027/api/Dashboard/customer').subscribe({
      next: (data: any) => {
        this.stats = data;
        this.loading = false;
        this.prepareChartData(data);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching customer dashboard data', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private prepareChartData(data: any) {
    if (data.myClaimsByStatus) {
      // Map dictionary keys to labels and values to data
      const labels = Object.keys(data.myClaimsByStatus);
      const values = Object.values(data.myClaimsByStatus) as number[];

      this.claimsChartData.labels = labels;
      this.claimsChartData.datasets[0].data = values;
    }
  }
}