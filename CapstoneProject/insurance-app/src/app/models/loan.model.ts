export interface LoanResponseDto {
    id: number;
    policyAssignmentId: number;
    policyNumber: string;
    planName: string;
    customerName: string;
    customerEmail: string;
    loanAmount: number;
    interestRate: number;
    outstandingBalance: number;
    totalInterestPaid: number;
    status: string;
    loanDate: string;
    closedDate?: string;
    repayments: LoanRepaymentDto[];
}

export interface LoanRepaymentDto {
    id: number;
    amount: number;
    principalPaid: number;
    interestPaid: number;
    balanceAfter: number;
    repaymentDate: string;
}

export interface ApplyLoanDto {
    policyAssignmentId: number;
}

export interface RepayLoanDto {
    policyLoanId: number;
    amount: number;
    paymentMethod: string;
}
