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
    if (filter.hasMaturityBenefit) 
      params = params.set('hasMaturityBenefit', filter.hasMaturityBenefit);
    if (filter.isReturnOfPremium) 
      params = params.set('isReturnOfPremium', filter.isReturnOfPremium);
    if (filter.maxNominees != null)
      params = params.set('maxNominees', filter.maxNominees);
    if (filter.maxPolicyMembersAllowed != null)
      params = params.set('maxPolicyMembersAllowed', filter.maxPolicyMembersAllowed);
    if (filter.hasBonus)
      params = params.set('hasBonus', filter.hasBonus);
    if (filter.hasLoanFacility)
      params = params.set('hasLoanFacility', filter.hasLoanFacility);
    if (filter.coverageIncreasing)
      params = params.set('coverageIncreasing', filter.coverageIncreasing);
    if (filter.maxLoanInterestRate != null)
      params = params.set('maxLoanInterestRate', filter.maxLoanInterestRate);
    if (filter.minMaxLoanPercentage != null)
      params = params.set('minMaxLoanPercentage', filter.minMaxLoanPercentage);
    if (filter.maxLoanEligibleAfterYears != null)
      params = params.set('maxLoanEligibleAfterYears', filter.maxLoanEligibleAfterYears);
    if (filter.minCoverageUntilAge != null)
      params = params.set('minCoverageUntilAge', filter.minCoverageUntilAge);
    if (filter.minCoverageIncreaseRate != null)
      params = params.set('minCoverageIncreaseRate', filter.minCoverageIncreaseRate);

    return this.http.get<any[]>(`${this.baseUrl}/filter`, { params });
  }
}