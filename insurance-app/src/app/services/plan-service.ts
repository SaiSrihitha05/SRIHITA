import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PlanService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7027/api/Plans';

  // Admin sees all, Customer sees active only based on backend logic
  getPlans(): Observable<any[]> {
    return this.http.get<any[]>(this.baseUrl);
  }

  getPlanById(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`);
  }

  createPlan(planData: any): Observable<any> {
    return this.http.post<any>(this.baseUrl, planData);
  }

  updatePlan(id: number, planData: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, planData);
  }

  deletePlan(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  getFilteredPlans(filter: any): Observable<any[]> {
    let params = new HttpParams();
    
    if (filter.planType) 
      params = params.set('planType', filter.planType);
    if (filter.age != null) 
      params = params.set('age', filter.age);
    if (filter.coverageAmount != null) 
      params = params.set('coverageAmount', filter.coverageAmount);
    if (filter.termYears != null) 
      params = params.set('termYears', filter.termYears);
    if (filter.hasMaturityBenefit != null) 
      params = params.set('hasMaturityBenefit', filter.hasMaturityBenefit);
    if (filter.isReturnOfPremium != null) 
      params = params.set('isReturnOfPremium', filter.isReturnOfPremium);

    return this.http.get<any[]>(`${this.baseUrl}/filter`, { params });
  }
}