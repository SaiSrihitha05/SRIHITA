import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ClaimService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7027/api/Claims';

  // Admin
  getAllClaims(): Observable<any[]> {
    return this.http.get<any[]>(this.baseUrl);
  }

  assignClaimsOfficer(claimId: number, officerId: number): Observable<any> {
    return this.http.patch(
      `${this.baseUrl}/${claimId}/assign-officer`,
      { claimsOfficerId: officerId }
    );
  }

  // ClaimsOfficer
  getMyAssignedClaims(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/my-assigned-claims`);
  }

  processClaim(claimId: number, dto: {
    status: string,
    remarks: string,
    settlementAmount?: number
  }): Observable<any> {
    return this.http.patch(`${this.baseUrl}/${claimId}/process`, dto);
  }
  updateClaimStatus(claimId: number, statusDto: { status: string, remarks: string }): Observable<any> {
    return this.http.patch(`${this.baseUrl}/${claimId}/status`, statusDto);
  }

  // Customer
  fileClaim(formData: FormData): Observable<any> {
    return this.http.post(this.baseUrl, formData);
  }

  getMyClaims(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/my-claims`);
  }

  getClaimById(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }
}