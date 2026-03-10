import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7027/api/Payments';

  makePayment(dto: any): Observable<any> {
    return this.http.post(this.baseUrl, dto);
  }
  getMyPayments(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/my-payments`);
  }

  downloadInvoice(paymentId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${paymentId}/invoice`, { responseType: 'blob' });
  }
  getPaymentsByPolicy(policyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/policy/${policyId}`);
  }
  getPremiumForecast(): Observable<any[]> {
  // We use the policy data to project future dates
  return this.http.get<any[]>(`https://localhost:7027/api/Policies/my-policies`);
}

}