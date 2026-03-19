import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
  selector: 'app-agent-dashboard',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './dashboard.html'
})
export class AgentDashboard implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  stats: any = null;
  loading = true;

  formatCurrency(value: number): string {
    if (!value) return '₹0';
    if (value >= 10000000) {
      return `₹${(value / 10000000).toFixed(2)} Crore`;
    } else if (value >= 100000) {
      return `₹${(value / 100000).toFixed(2)} Lakh`;
    }
    return `₹${value.toLocaleString()}`;
  }

  // Chart Properties
  public earningsTrendData: ChartData<'line'> = {
    labels: [],
    datasets: [{
      data: [],
      label: 'Commission (₹)',
      borderColor: '#75013f',
      backgroundColor: 'rgba(117, 1, 63, 0.1)',
      fill: true,
      tension: 0.4
    }]
  };
  public earningsTrendOptions: ChartConfiguration['options'] = {
    responsive: true,
    scales: { y: { beginAtZero: true } }
  };

  public salesVolumeData: ChartData<'bar'> = {
    labels: [],
    datasets: [{
      data: [],
      label: 'Policies Sold',
      backgroundColor: '#fe3082'
    }]
  };
  public salesVolumeOptions: ChartConfiguration['options'] = {
    responsive: true,
    scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
  };

  public healthData: ChartData<'doughnut'> = {
    labels: ['Active', 'Pending'],
    datasets: [{
      data: [],
      backgroundColor: ['#10b981', '#fb923c']
    }]
  };

  public planMixData: ChartData<'pie'> = {
    labels: [],
    datasets: [{
      data: [],
      backgroundColor: ['#75013f', '#fe3082', '#3b82f6', '#fb923c', '#10b981']
    }]
  };

  ngOnInit() {
    this.fetchAgentStats();
  }

  fetchAgentStats() {
    const token = localStorage.getItem('token');
    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    this.http.get('https://localhost:7027/api/Dashboard/agent').subscribe({
      next: (data: any) => {
        this.stats = data;
        this.prepareChartData(data);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Frontend Error fetching agent stats:', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private prepareChartData(data: any) {
    if (data.monthlyCommission && data.monthlyCommission.length > 0) {
      this.earningsTrendData.labels = data.monthlyCommission.map((m: any) => m.monthName);
      this.earningsTrendData.datasets[0].data = data.monthlyCommission.map((m: any) => m.commissionEarned);

      this.salesVolumeData.labels = data.monthlyCommission.map((m: any) => m.monthName);
      this.salesVolumeData.datasets[0].data = data.monthlyCommission.map((m: any) => m.policiesSold);
    }

    if (data.policiesByPlanType && data.policiesByPlanType.length > 0) {
      this.planMixData.labels = data.policiesByPlanType.map((p: any) => p.planType);
      this.planMixData.datasets[0].data = data.policiesByPlanType.map((p: any) => p.count);
    }

    this.healthData.datasets[0].data = [data.activePolicies || 0, data.pendingPolicies || 0];
  }
}
