import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface KycResponse {
  isSuccess: boolean;
  extractedName: string;
  extractedIdNumber: string;
  kycStatus: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class KycService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7027/api/Kyc'; // Updated to match policy-service

  // Shared Validation Patterns (normalized keys: uppercase, no spaces)
  static readonly ID_PATTERNS: { [key: string]: RegExp } = {
    'AADHARCARD': /^[2-9]\d{11}$/,
    'AADHAARCARD': /^[2-9]\d{11}$/,
    'AADHAAR': /^[2-9]\d{11}$/,
    'PAN': /^[A-Z]{5}[0-9]{4}[A-Z]$/,
    'PANCARD': /^[A-Z]{5}[0-9]{4}[A-Z]$/
  };

  verifyCustomerKyc(targetId: number, idProofType: string, idProofNumber: string, name: string, file: File): Observable<KycResponse> {
    const formData = new FormData();
    formData.append('TargetId', targetId.toString());
    formData.append('IdProofType', idProofType);
    formData.append('IdProofNumber', idProofNumber);
    formData.append('FullName', name);
    formData.append('File', file);

    return this.http.post<KycResponse>(`${this.apiUrl}/customer`, formData);
  }

  verifyMemberKyc(targetId: number, idProofType: string, idProofNumber: string, name: string, file: File): Observable<KycResponse> {
    const formData = new FormData();
    formData.append('TargetId', targetId.toString());
    formData.append('IdProofType', idProofType);
    formData.append('IdProofNumber', idProofNumber);
    formData.append('FullName', name);
    formData.append('File', file);

    return this.http.post<KycResponse>(`${this.apiUrl}/member`, formData); // Simplified path
  }

  // Temporary endpoint for new members (not yet in DB)
  verifyNewMemberKyc(idProofType: string, idProofNumber: string, name: string, file: File): Observable<KycResponse> {
    const formData = new FormData();
    formData.append('TargetId', '0'); // 0 indicates a new member/customer for validation-only
    formData.append('IdProofType', idProofType);
    formData.append('IdProofNumber', idProofNumber);
    formData.append('FullName', name);
    formData.append('File', file);

    return this.http.post<KycResponse>(`${this.apiUrl}/customer`, formData);
  }

  verifyDeathCertificate(file: File, certificateNumber: string, dateOfDeath: string, deceasedName: string): Observable<KycResponse> {
    const formData = new FormData();
    formData.append('File', file);
    formData.append('CertificateNumber', certificateNumber);
    formData.append('DateOfDeath', dateOfDeath);
    formData.append('DeceasedName', deceasedName);

    return this.http.post<KycResponse>(`${this.apiUrl}/verify-death-certificate`, formData);
  }

  verifyNomineeIdentity(file: File, expectedName: string): Observable<KycResponse> {
    const formData = new FormData();
    formData.append('File', file);
    formData.append('ExpectedName', expectedName);

    return this.http.post<KycResponse>(`${this.apiUrl}/verify-nominee`, formData);
  }
}
