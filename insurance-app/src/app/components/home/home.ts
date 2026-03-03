import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './home.html'
})
export class Home {
  // Simple properties instead of signals
  name: string = '';
  email: string = '';
  phone: string = '';
  message: string = '';

  isSubmitting: boolean = false;
  isSubmitted: boolean = false;

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
    setTimeout(() => {
      this.isSubmitting = false;
      this.isSubmitted = true;
      // Reset fields
      this.name = '';
      this.email = '';
      this.phone = '';
      this.message = '';
    }, 1500);
  }
}