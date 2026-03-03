import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../services/payment-service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-payment-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './payment-history.html'
})
export class PaymentHistory implements OnInit {
  private paymentService = inject(PaymentService);
  private cdr = inject(ChangeDetectorRef);

  payments: any[] = [];
  groupedPayments: { [key: string]: any[] } = {};
  loading = true;

  ngOnInit() {
    this.loadHistory();     // Loads the table
    this.loadUpcomingTwo(); // Loads the 2 Priority Cards
    this.loadForecast();    // Loads the full 2026 Forecast
  }

  loadHistory() {
    this.paymentService.getMyPayments().subscribe({
      next: (data) => {
        this.payments = data;
        this.groupData();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error("Failed to load payments", err);
        this.loading = false;
      }
    });
  }

  private groupData() {
    // Organizes the flat PaymentResponseDto list into a policy-keyed object
    this.groupedPayments = this.payments.reduce((acc, payment) => {
      const key = payment.policyNumber || 'General';
      if (!acc[key]) acc[key] = [];
      acc[key].push(payment);
      return acc;
    }, {});
  }
  forecastData: any[] = [];

  loadForecast() {
    this.paymentService.getPremiumForecast().subscribe(policies => {
      const projection: any[] = [];
      const today = new Date();
      today.setHours(0, 0, 0, 0); // Normalize to start of day
      const yearEnd = new Date(2026, 11, 31);

      policies.forEach(policy => {
        if (policy.status === 'Active') {
          let nextDate = new Date(policy.nextDueDate);

          // Project dates until end of year
          while (nextDate <= yearEnd) {
            if (nextDate >= today) {
              projection.push({
                date: new Date(nextDate),
                amount: policy.totalPremiumAmount,
                planName: policy.planName,
                policyNumber: policy.policyNumber
              });
            }
            // Increment based on frequency
            this.advanceDate(nextDate, policy.premiumFrequency);
          }
        }
      });
      this.forecastData = projection.sort((a, b) => a.date.getTime() - b.date.getTime());
      this.cdr.detectChanges();
    });
  }

  private advanceDate(date: Date, frequency: string) {
    if (frequency === 'Monthly') date.setMonth(date.getMonth() + 1);
    else if (frequency === 'Quarterly') date.setMonth(date.getMonth() + 3);
    else if (frequency === 'Yearly') date.setFullYear(date.getFullYear() + 1);
  }
  getTotalForecast(): number {
    // Sums up all the 'amount' values in the forecastData array
    return this.forecastData.reduce((acc, item) => acc + item.amount, 0);
  }
  // Add these to your PaymentHistory class
  upcomingPayments: any[] = [];

  loadUpcomingTwo() {
    this.paymentService.getPremiumForecast().subscribe({
      next: (policies) => {
        const allFutureDates: any[] = [];
        const today = new Date();
        // Set to start of today for accurate comparison
        today.setHours(0, 0, 0, 0);

        policies.forEach(policy => {
          if (policy.status === 'Active') {
            // Check the current NextDueDate from the database
            let nextDate = new Date(policy.nextDueDate);

            // We only want the next 2 occurrences total across all plans
            // Project the next few instances for each policy to compare them
            for (let i = 0; i < 2; i++) {
              allFutureDates.push({
                date: new Date(nextDate),
                amount: policy.totalPremiumAmount,
                planName: policy.planName,
                policyNumber: policy.policyNumber,
                id: policy.id
              });
              this.advanceDate(nextDate, policy.premiumFrequency);
            }
          }
        });

        // Sort by date and take only the top 2 that are today or in the future
        this.upcomingPayments = allFutureDates
          .filter(p => p.date >= today)
          .sort((a, b) => a.date.getTime() - b.date.getTime())
          .slice(0, 2);

        this.cdr.detectChanges();
      }
    });
  }
  download(paymentId: number) {
    this.paymentService.downloadInvoice(paymentId).subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Invoice_${paymentId}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    });
  }

  objectKeys(obj: any) {
    return Object.keys(obj);
  }
}