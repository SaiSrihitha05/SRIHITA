import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PolicyService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7027/api/Policies';

  // Admin: Get all policies in the system
  getAllPolicies(): Observable<any[]> {
    return this.http.get<any[]>(this.baseUrl);
  }

  // Admin: Assign an agent to a specific policy
  assignAgent(policyId: number, agentId: number): Observable<any> {
    return this.http.patch(`${this.baseUrl}/${policyId}/assign-agent`, { agentId });
  }

  // Shared: Get full details of a single policy
  getPolicyById(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }
  // Agent: Fetch policies assigned to the logged-in agent
  getAgentPolicies(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/my-assigned-policies`);
  }

  // Agent: Update status of a specific policy (Pending, Active, etc.)
  updatePolicyStatus(id: number, statusDto: { status: string, remarks: string }): Observable<any> {
    return this.http.patch(`${this.baseUrl}/${id}/status`, statusDto);
  }

  //Customer
  buyPolicy(formData: FormData): Observable<any> {
    return this.http.post(`${this.baseUrl}`, formData);
  }
  getMyPolicies(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/my-policies`);
  }
  downloadFile(fileId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/download-document/${fileId}`, 
      { responseType: 'blob' });
  }
  cancelPolicy(policyId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/${policyId}/cancel`, {});
  }
  saveDraft(dto: any): Observable<any> {
  return this.http.post(`${this.baseUrl}/draft`, dto);
}

updateDraft(id: number, dto: any): Observable<any> {
  return this.http.put(`${this.baseUrl}/draft/${id}`, dto);
}

getMyDrafts(): Observable<any[]> {
  return this.http.get<any[]>(`${this.baseUrl}/my-drafts`);
}

deleteDraft(id: number): Observable<any> {
  return this.http.delete(`${this.baseUrl}/draft/${id}`);
}

submitDraft(id: number, fd: FormData): Observable<any> {
  return this.http.post(`${this.baseUrl}/draft/${id}/submit`, fd);
}

  downloadPolicyApplication(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/download-application`,
      { responseType: 'blob' });
  }

  replaceDocument(documentId: number, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.baseUrl}/replace-document/${documentId}`, formData);
  }

  getReinstatementQuote(policyId: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/${policyId}/reinstatement-quote`);
  }

  reinstatePolicy(policyId: number, paymentReference: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${policyId}/reinstate`, { paymentReference });
  }

  remindExpiry(policyId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/${policyId}/remind-expiry`, {});
  }
}