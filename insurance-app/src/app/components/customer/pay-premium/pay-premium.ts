import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../../services/payment-service';
import { PolicyService } from '../../../services/policy-service';

@Component({
  selector: 'app-pay-premium',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pay-premium.html'
})
export class PayPremium implements OnInit {
  private route = inject(ActivatedRoute);
  public router = inject(Router);
  private paymentService = inject(PaymentService);
  private policyService = inject(PolicyService);
  private cdr = inject(ChangeDetectorRef);

  policyId: number = 0;
  policy: any = null;
  paymentSuccess = false;
  newPaymentId: number | null = null;

  paymentData = {
    policyAssignmentId: 0,
    paymentMethod: 'UPI', // UPI, Card, NetBanking
    extraInstallments: 0
  };

  processing = false;

  ngOnInit() {
    this.policyId = Number(this.route.snapshot.queryParams['policyId']);
    this.paymentData.policyAssignmentId = this.policyId;

    this.policyService.getPolicyById(this.policyId).subscribe(data => {
      this.policy = data;
      this.cdr.detectChanges();
    });
  }

  get installmentLabel(): string {
    if (!this.policy) return 'Month';
    const freq = this.policy.premiumFrequency;
    if (freq === 'Monthly') return 'Month';
    if (freq === 'Quarterly') return 'Quarter';
    if (freq === 'Yearly') return 'Year';
    return 'Period';
  }

  get installmentOptions(): { value: number, label: string }[] {
    const label = this.installmentLabel;
    const options = [{ value: 0, label: `Current ${label} Only` }];

    // Customize options based on frequency
    let counts = [1, 2, 5]; // Default for monthly
    if (this.policy?.premiumFrequency === 'Quarterly') counts = [1, 2, 3];
    if (this.policy?.premiumFrequency === 'Yearly') counts = [1, 2];

    counts.forEach(c => {
      options.push({ value: c, label: `Next ${c} ${label}${c > 1 ? 's' : ''}` });
    });

    return options;
  }

  get totalAmount(): number {
    if (!this.policy) return 0;
    return this.policy.totalPremiumAmount * (1 + this.paymentData.extraInstallments);
  }

  submitPayment() {
    this.processing = true;
    console.log("Submitting Payment DTO:", this.paymentData);

    this.paymentService.makePayment(this.paymentData).subscribe({
      next: (res) => {
        // 1. Reset processing first
        this.processing = false;

        // 2. Set the success state
        this.newPaymentId = res.id;
        this.paymentSuccess = true;


        // 4. Scroll to top so they see the success message
        window.scrollTo({ top: 0, behavior: 'smooth' });
      },
      error: (err) => {
        this.processing = false;
        console.error("Payment API Error Details:", err.error);
        const errorMessage = err.error?.message || err.error || "Payment failed";
        alert(`Transaction Error: ${errorMessage}`);

      }
    });
  }
  downloadInvoice() {
    if (this.newPaymentId) {
      this.paymentService.downloadInvoice(this.newPaymentId).subscribe(blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Invoice_${this.newPaymentId}.pdf`;
        document.body.appendChild(a); // Append to body for better browser compatibility
        a.click();

        // Clean up
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      });
    }
  }
}