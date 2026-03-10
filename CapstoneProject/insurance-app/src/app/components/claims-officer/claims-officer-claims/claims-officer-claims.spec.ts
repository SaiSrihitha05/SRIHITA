Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => { },
    removeListener: () => { },
    addEventListener: () => { },
    removeEventListener: () => { },
    dispatchEvent: () => false,
  }),
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClaimsOfficerClaims } from './claims-officer-claims';
import { ClaimService } from '../../../services/claim-service';
import { of } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('ClaimsOfficerClaims', () => {
  let component: ClaimsOfficerClaims;
  let fixture: ComponentFixture<ClaimsOfficerClaims>;
  let mockClaimService: any;

  const mockClaims = [
    { id: 1, status: 'Submitted', claimAmount: 5000, policyNumber: 'POL123' },
    { id: 2, status: 'Settled', claimAmount: 10000, policyNumber: 'POL456' }
  ];

  beforeEach(async () => {
    // 2. Mock the ClaimService specifically for Pola Yashasree's dashboard
    mockClaimService = jasmine.createSpyObj('ClaimService', ['getMyAssignedClaims', 'processClaim']);
    mockClaimService.getMyAssignedClaims.and.returnValue(of(mockClaims));

    await TestBed.configureTestingModule({
      imports: [ClaimsOfficerClaims, CommonModule, FormsModule],
      providers: [
        { provide: ClaimService, useValue: mockClaimService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]), // 3. Provides routing context for the side-bar and layouts
        ChangeDetectorRef
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimsOfficerClaims);
    component = fixture.componentInstance;
    fixture.detectChanges(); // 4. Triggers ngOnInit and service calls
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should load assigned claims on init', () => {
    expect(mockClaimService.getMyAssignedClaims).toHaveBeenCalled();
    expect(component.claims.length).toBe(2);
    expect(component.loading).toBeFalse();
  });

  it('should open process modal and reset form', () => {
    const testClaim = mockClaims[0];
    component.openProcessModal(testClaim);

    expect(component.selectedClaim).toEqual(testClaim);
    expect(component.showProcessModal).toBeTrue();
    expect(component.processForm.remarks).toBe('');
    expect(component.processForm.status).toBe('Settled');
  });

  it('should Close modal and clear selected claim', () => {
    component.showProcessModal = true;
    component.closeModal();
    expect(component.showProcessModal).toBeFalse();
    expect(component.selectedClaim).toBeNull();
  });

  it('should alert if remarks are missing during submission', () => {
    spyOn(window, 'alert');
    component.processForm.remarks = '';
    component.submitProcess();
    expect(window.alert).toHaveBeenCalledWith('Remarks are required');
  });

  it('should alert if settlement amount is missing for Settled status', () => {
    spyOn(window, 'alert');
    component.selectedClaim = { id: 1 };
    component.processForm.status = 'Settled';
    component.processForm.remarks = 'Valid remarks';
    component.processForm.settlementAmount = null;

    component.submitProcess();
    expect(window.alert).toHaveBeenCalledWith('Settlement amount is required when approving or settling a claim');
  });

  it('should call processClaim and reload data on success', () => {
    spyOn(window, 'alert');
    mockClaimService.processClaim.and.returnValue(of({}));
    const reloadSpy = spyOn(component, 'loadMyClaims');

    component.selectedClaim = { id: 1 };
    component.processForm = {
      status: 'Approved',
      remarks: 'Looks good',
      settlementAmount: 4500
    };

    component.submitProcess();

    expect(mockClaimService.processClaim).toHaveBeenCalledWith(1, {
      status: 'Approved',
      remarks: 'Looks good',
      settlementAmount: 4500
    });
    expect(window.alert).toHaveBeenCalled();
    expect(reloadSpy).toHaveBeenCalled();
  });

  it('should return false in canProcess for Settled or Rejected claims', () => {
    const settledClaim = { status: 'Settled' };
    const rejectedClaim = { status: 'Rejected' };
    const activeClaim = { status: 'UnderReview' };

    expect(component.canProcess(settledClaim)).toBeFalse();
    expect(component.canProcess(rejectedClaim)).toBeFalse();
    expect(component.canProcess(activeClaim)).toBeTrue();
  });

  it('should return correct badge classes for statuses', () => {
    expect(component.getStatusBadgeClass('Submitted')).toBe('bg-blue-500');
    expect(component.getStatusBadgeClass('UnderReview')).toBe('bg-amber-500');
    expect(component.getStatusBadgeClass('Settled')).toBe('bg-green-600');
  });
});