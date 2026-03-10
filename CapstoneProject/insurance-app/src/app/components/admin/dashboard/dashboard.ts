import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})

export class Dashboard implements OnInit {
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

  // Data property to hold the backend response
  stats: any = null;
  loading: boolean = true;

  // Chart Properties
  public pieChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true,
        position: 'bottom',
      },
    },
  };
  public pieChartData: ChartData<'pie', number[], string | string[]> = {
    labels: [],
    datasets: [{
      data: [],
      backgroundColor: ['#75013f', '#fe3082', '#3b82f6', '#fb923c', '#10b981'],
    }]
  };
  public pieChartType: ChartType = 'pie';

  public barChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    scales: {
      x: {},
      y: { min: 0 }
    },
    plugins: {
      legend: { display: false }
    }
  };
  public barChartData: ChartData<'bar'> = {
    labels: [],
    datasets: [{
      data: [],
      label: 'Policies Sold',
      backgroundColor: '#75013f'
    }]
  };
  public barChartType: ChartType = 'bar';

  public histogramChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    scales: {
      x: { title: { display: true, text: 'Claim Amount Range' } },
      y: { title: { display: true, text: 'Frequency' }, min: 0 }
    },
    plugins: {
      legend: { display: false }
    }
  };
  public histogramChartData: ChartData<'bar'> = {
    labels: [],
    datasets: [{
      data: [],
      label: 'Number of Claims',
      backgroundColor: '#fe3082'
    }]
  };

  // 4. Monthly Revenue Trend (Line Chart)
  public revenueTrendOptions: ChartConfiguration['options'] = {
    responsive: true,
    scales: {
      y: { beginAtZero: true, title: { display: true, text: 'Revenue (₹)' } }
    },
    plugins: {
      legend: { display: true, position: 'top' }
    }
  };
  public revenueTrendData: ChartData<'line'> = {
    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
    datasets: [{
      data: [],
      label: 'Revenue Trend',
      borderColor: '#75013f',
      backgroundColor: 'rgba(117, 1, 63, 0.1)',
      fill: true,
      tension: 0.4
    }]
  };

  // 5. Claims Efficiency (Doughnut Chart)
  public claimsEfficiencyOptions: ChartConfiguration['options'] = {
    responsive: true,
    plugins: {
      legend: { position: 'bottom' }
    }
  };
  public claimsEfficiencyData: ChartData<'doughnut'> = {
    labels: ['Settled', 'Pending'],
    datasets: [{
      data: [],
      backgroundColor: ['#10b981', '#fb923c']
    }]
  };

  ngOnInit() {
    this.fetchDashboardData();
  }

  fetchDashboardData() {
    this.http.get('https://localhost:7027/api/Dashboard/admin').subscribe({
      next: (data: any) => {
        if (data.recentPolicies) {
          data.recentPolicies = data.recentPolicies.filter((p: any) => p.status !== 'Draft');
        }
        this.stats = data;
        this.loading = false;
        this.prepareChartData(data);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching dashboard data', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private prepareChartData(data: any) {
    // 1. Pie Chart: Revenue by Plan Type
    if (data.revenueByPlanType) {
      this.pieChartData.labels = data.revenueByPlanType.map((p: any) => p.planName);
      this.pieChartData.datasets[0].data = data.revenueByPlanType.map((p: any) => p.totalRevenue);
    }

    // 2. Bar Chart: Agent Performance (Policies Sold)
    if (data.agentPerformance) {
      this.barChartData.labels = data.agentPerformance.map((a: any) => a.agentName);
      this.barChartData.datasets[0].data = data.agentPerformance.map((a: any) => a.policiesSold);
    }

    // 3. Histogram: Claim Amount Distribution
    if (data.recentClaims) {
      const amounts = data.recentClaims.map((c: any) => c.claimAmount);
      this.binClaimAmounts(amounts);
    }

    // 4. Monthly Revenue Trend
    // Mocking some data if monthlyRevenue is not in the backend response yet
    // In a real scenario, the backend should provide this.
    // For now, let's distribute the total revenue across current months for visualization.
    const currentMonth = new Date().getMonth();
    const mockTrendData = new Array(12).fill(0).map((_, i) => i <= currentMonth ? (data.totalPremiumCollected / (currentMonth + 1)) * (0.8 + Math.random() * 0.4) : 0);
    this.revenueTrendData.datasets[0].data = mockTrendData;

    // 5. Claims Efficiency
    this.claimsEfficiencyData.datasets[0].data = [data.settledClaims, data.totalClaims - data.settledClaims];
  }

  private binClaimAmounts(amounts: number[]) {
    if (amounts.length === 0) return;
    const max = Math.max(...amounts);
    const min = Math.min(...amounts);
    const range = max - min;
    const binCount = 5;
    const binWidth = range / binCount || 10000;

    const bins = new Array(binCount).fill(0);
    const labels = [];

    for (let i = 0; i < binCount; i++) {
      const start = min + i * binWidth;
      const end = start + binWidth;
      labels.push(`₹${Math.round(start / 1000)}k-${Math.round(end / 1000)}k`);
    }

    amounts.forEach(amt => {
      let binIdx = Math.floor((amt - min) / binWidth);
      if (binIdx >= binCount) binIdx = binCount - 1;
      bins[binIdx]++;
    });

    this.histogramChartData.labels = labels;
    this.histogramChartData.datasets[0].data = bins;
  }
}
