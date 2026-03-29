import { Injectable } from '@angular/core';

export interface BenefitInfo {
  label: string;
  key: string;
  explanation: string;
}

@Injectable({ providedIn: 'root' })
export class BenefitGlossaryService {

  readonly benefits: BenefitInfo[] = [
    {
      key: 'maturityBenefit',
      label: 'Maturity Benefit',
      explanation:
        'A lump sum amount paid to you when the policy term ends ' +
        'and you are still alive. You get back more than you paid in premiums.'
    },
    {
      key: 'returnPremium',
      label: 'Return of Premium',
      explanation:
        'If no claim is made during the policy term, all the premiums ' +
        'you paid are returned to you at the end. Zero loss even if nothing happens.'
    },
    {
      key: 'bonus',
      label: 'Bonus and Profits',
      explanation:
        'The insurer shares a portion of its profits with you annually. ' +
        'This bonus is added to your sum assured and paid out at maturity or on a claim.'
    },
    {
      key: 'loanFacility',
      label: 'Loan Facility',
      explanation:
        'You can borrow money against your policy\'s surrender value ' +
        'without cancelling it. Useful for emergencies without breaking the policy.'
    },
    {
      key: 'increasingCover',
      label: 'Increasing Cover',
      explanation:
        'Your sum assured automatically increases every year, usually by ' +
        '5–10%, to keep up with inflation. Your family gets more protection over time.'
    }
  ];

  getBenefitsForPlan(plan: any): BenefitInfo[] {
    const result: BenefitInfo[] = [];
    if (plan.hasBonus) result.push(this.benefits.find(b => b.key === 'bonus')!);
    if (plan.hasLoanFacility) result.push(this.benefits.find(b => b.key === 'loanFacility')!);
    
    // CoverageIncreasing / CoverageUntilAge
    if (plan.isCoverageUntilAge || plan.coverageIncreasing) {
      result.push(this.benefits.find(b => b.key === 'increasingCover')!);
    }

    // Add maturity and returnPremium based on plan type or specific flags
    if (plan.hasMaturityBenefit || ['Endowment', 'Savings', 'WholeLife'].includes(plan.planType)) {
      result.unshift(this.benefits.find(b => b.key === 'maturityBenefit')!);
    }

    if (plan.isReturnOfPremium) {
       result.push(this.benefits.find(b => b.key === 'returnPremium')!);
    }

    return result;
  }
}
