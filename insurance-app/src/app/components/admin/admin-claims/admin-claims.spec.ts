import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminClaims } from './admin-claims';
import { ClaimService } from '../../../services/claim-service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { ChangeDetectorRef } from '@angular/core';

describe('AdminClaims', () => {
  let component: AdminClaims;
  let fixture: ComponentFixture<AdminClaims>;
  let mockClaimService: any;
  let httpTestingController: HttpTestingController;

  const mockClaims = [{ id: 101, policyNumber: 'POL789', status: 'Submitted' }];
  const mockOfficers = [{ id: 1, name: 'Manoj bhai' }];

  beforeEach(async () => {
    // Jasmine globals are native to Karma
    mockClaimService = jasmine.createSpyObj('ClaimService', ['getAllClaims', 'assignClaimsOfficer']);
    mockClaimService.getAllClaims.and.returnValue(of(mockClaims));

    await TestBed.configureTestingModule({
      imports: [AdminClaims],
      providers: [
        { provide: ClaimService, useValue: mockClaimService },
        provideHttpClient(),
        provideHttpClientTesting(),
        ChangeDetectorRef
      ]
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AdminClaims);
    component = fixture.componentInstance;
    
    // Trigger ngOnInit
    fixture.detectChanges(); 
    
    // Resolve the HttpClient call in the Karma browser
    const req = httpTestingController.expectOne('https://localhost:7027/api/Users/claims-officers');
    req.flush(mockOfficers);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load claims and officers on init', () => {
    expect(mockClaimService.getAllClaims).toHaveBeenCalled();
    expect(component.claims.length).toBe(1);
    expect(component.officers.length).toBe(1);
  });
});