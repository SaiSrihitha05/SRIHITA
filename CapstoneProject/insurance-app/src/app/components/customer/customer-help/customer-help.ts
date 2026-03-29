import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { BenefitGlossaryService } from '../../../services/benefit-glossary.service';

@Component({
  selector: 'app-customer-help',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './customer-help.html'
})
export class CustomerHelp {
  public glossary = inject(BenefitGlossaryService);
  sections = [
    {
      id: 'glossary',
      title: 'Insurance Terms Explained',
      icon: '📖',
      content: 'Understand common insurance benefits and terminology used in our protection plans.',
      benefits: this.glossary.benefits
    },
    {
      id: 'apply',
      title: 'How to Apply for Policy',
      icon: '🛡️',
      steps: [
        { title: 'Step 1', description: 'Browse available insurance plans.', icon: '🔍', tip: 'Compare multiple plans to find the best fit.' },
        { title: 'Step 2', description: 'Select a plan and click "Buy Policy".', icon: '🛒', tip: 'You can save drafts and return later.' },
        { title: 'Step 3', description: 'Enter coverage amount, policy term and member details.', icon: '✍️', tip: 'Ensure member ages meet the plan requirements.' },
        { title: 'Step 4', description: 'Add nominee information.', icon: '👥', tip: 'You can add up to 5 nominees for most plans.' },
        { title: 'Step 5', description: 'Upload required documents.', icon: '📄', tip: 'Clear scans of ID and Income proof are recommended.' },
        { title: 'Step 6', description: 'Your policy will be reviewed and activated by the agent.', icon: '👨‍💼', tip: 'Usually takes 24-48 business hours.' },
        { title: 'Step 7', description: 'Pay premium', icon: '💳', tip: 'Multiple payment modes available.' }
      ],
      cta: { label: 'Explore All Policies', link: '/customer-dashboard/explore-plans', icon: '🚀' }
    },
    {
      id: 'claim',
      title: 'How to File a Claim',
      icon: '📝',
      steps: [
        { title: 'Step 1', description: 'Select the member for whom the claim is filed', icon: '👤', tip: 'Only active members are eligible.' },
        { title: 'Step 2', description: 'Enter death certificate details', icon: '📜', tip: 'Enter the certificate number and date correctly.' },
        { title: 'Step 3', description: 'Upload supporting documents', icon: '📁', tip: 'Death certificate and original policy bond needed.' },
        { title: 'Step 4', description: 'Submit the claim', icon: '🚀', tip: 'Double check all details before final submission.' },
        { title: 'Step 5', description: 'Claims officer will review your request', icon: '⚖️', tip: 'Track status updates on your claims dashboard.' }
      ],
      cta: { label: 'File a New Claim', link: '/customer-dashboard/my-policies', icon: '📑' }
    },
    {
      id: 'documents',
      title: 'Required Documents',
      icon: '📁',
      items: [
        { name: 'Identity Proof', details: 'Aadhar Card, PAN Card, or Passport' },
        { name: 'Income Proof', description: 'Salary Slips or ITR for high-value coverage' },
        { name: 'Death Claim Docs', description: 'Death Certificate and Original Policy Document' }
      ]
    },
    {
      id: 'support',
      title: 'Contact Support',
      icon: '📧',
      content: 'Need further assistance? Our team is here to help you around the clock.',
      contacts: [
        { type: 'Email', value: 'support@hartfordinsurance.com' },
        { type: 'Phone', value: '1800-456-7890 (Toll Free)' },
        { type: 'Working Hours', value: 'Mon - Sat: 9:00 AM to 6:00 PM' }
      ]
    }
  ];

  selectedSection = 'apply';

  selectSection(id: string) {
    this.selectedSection = id;
  }
}
