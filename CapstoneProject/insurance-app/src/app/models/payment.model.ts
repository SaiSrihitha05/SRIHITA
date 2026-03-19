export interface PaymentResponseDto {
    id: number;
    policyAssignmentId: number;
    policyNumber: string;
    amount: number;
    installmentsPaid: number;
    paymentDate: string;
    paymentMethod: string;
    transactionReference: string;
    status: string;
    commissionStatus: string;
    invoiceNumber: string;
    paymentType: string;
    nextDueDate: string;
    createdAt: string;
}

export interface CreatePaymentDto {
    policyAssignmentId: number;
    paymentMethod: string;
    extraInstallments: number;
}
