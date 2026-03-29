import { Component, inject, OnInit } from '@angular/core';
import { PlanService } from '../../services/plan-service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth-service';
import { ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './home.html'
})
export class Home implements OnInit {
  private authService = inject(AuthService);
  private planService = inject(PlanService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  featuredPlans: any[] = [];
  currentPlanIndex: number = 0;

  ngOnInit() {
    this.planService.getPlans().subscribe({
      next: (plans) => {
        this.featuredPlans = plans;
      },
      error: (err) => console.error('Error fetching plans:', err)
    });
  }

  nextPlan() {
    if (this.featuredPlans.length === 0) return;
    this.currentPlanIndex = (this.currentPlanIndex + 1) % this.featuredPlans.length;
  }

  prevPlan() {
    if (this.featuredPlans.length === 0) return;
    this.currentPlanIndex = (this.currentPlanIndex - 1 + this.featuredPlans.length) % this.featuredPlans.length;
  }

  getPlanImage(index: number): string {
    const fileIndex = (index % 6) + 1;
    const extension = fileIndex === 5 ? 'png' : 'jpg';
    return `plan${fileIndex}.${extension}`;
  }

  // Simple properties instead of signals
  name: string = '';
  email: string = '';
  phone: string = '';
  message: string = '';

  isSubmitting: boolean = false;
  isSubmitted: boolean = false;
  showExpertAlert: boolean = false;

  toggleExpertAlert() {
    this.showExpertAlert = !this.showExpertAlert;
  }

  /* Life insurance specific benefits shown in the "Why Us" section */
  whyUsCards = [
    { title: 'Legacy Protection', description: 'Ensure your loved ones are financially secure with our 200+ years of trust.' },
    { title: 'Hassle-Free Claims', description: 'We settle death and maturity claims with utmost priority and empathy.' },
    { title: 'Flexible Premiums', description: 'Life insurance plans designed to fit your unique financial goals and budget.' },
    { title: 'Maturity Benefits', description: 'Get guaranteed returns and bonuses on long-term endowment policies.' },
    { title: 'Tax Benefits', description: 'Save more with tax-efficient life insurance solutions under prevailing laws.' },
    { title: 'Expert Guidance', description: 'Our certified agents help you choose the right coverage for your family.' }
  ];

  /* Key performance indicators for the life insurance segment */
  stats = [
    { value: '200+', label: 'Years of Trust' },
    { value: '98.5%', label: 'Claim Settlement Ratio' },
    { value: '1.2M+', label: 'Active Policies' },
    { value: '24/7', label: 'Claim Support' }
  ];

  onSubmit() {
    this.isSubmitting = true;
    this.cdr.detectChanges();
    this.cdr.markForCheck();
    
    setTimeout(() => {
      this.isSubmitting = false;
      this.isSubmitted = true;
      // Reset fields
      this.name = '';
      this.email = '';
      this.phone = '';
      this.message = '';
      this.cdr.detectChanges();
      this.cdr.markForCheck();
    }, 1500);
  }

  get userRole(): string | null {
    return this.authService.getUserRole();
  }

  navigateToExplore() {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    const role = this.userRole;
    switch (role) {
      case 'Customer':
        this.router.navigate(['/customer-dashboard/explore-plans']);
        break;
      case 'Agent':
        this.router.navigate(['/agent-dashboard/explore-plans']);
        break;
      case 'Admin':
        this.router.navigate(['/admin-dashboard/plans']);
        break;
      case 'ClaimsOfficer':
        this.router.navigate(['/claims-officer-dashboard']);
        break;
      default:
        this.router.navigate(['/login']);
        break;
    }
  }
}