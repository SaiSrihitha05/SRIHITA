import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7027/api/Users';

  // Getters
  getCustomers(): Observable<any[]> { return this.http.get<any[]>(`${this.baseUrl}/customers`); }
  getAgents(): Observable<any[]> { return this.http.get<any[]>(`${this.baseUrl}/agents`); }
  getClaimsOfficers(): Observable<any[]> { return this.http.get<any[]>(`${this.baseUrl}/claims-officers`); }

  // Create Staff
  createAgent(dto: any): Observable<any> { return this.http.post(`${this.baseUrl}/agents`, dto); }
  createClaimsOfficer(dto: any): Observable<any> { return this.http.post(`${this.baseUrl}/claims-officers`, dto); }

  // Actions
  deleteUser(id: number): Observable<any> { return this.http.delete(`${this.baseUrl}/${id}`); }

  //Profile
  getProfile(): Observable<any> {
  return this.http.get(`${this.baseUrl}/profile`);
  }

  updateProfile(profileData: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/profile`, profileData);
  }
}