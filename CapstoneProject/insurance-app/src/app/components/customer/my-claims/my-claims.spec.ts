import { TestBed, ComponentFixture } from '@angular/core/testing';
import { MyClaims } from './my-claims';
import { ClaimService } from '../../../services/claim-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('MyClaims', () => {
  let component: MyClaims;
  let fixture: ComponentFixture<MyClaims>;
  let claimService: ClaimService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyClaims],
      providers: [
        ClaimService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MyClaims);
    component = fixture.componentInstance;
    claimService = TestBed.inject(ClaimService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load claims on init', () => {
    const mockClaims = [{ id: 1, policyNumber: 'POL-001', status: 'Submitted' }];
    spyOn(claimService, 'getMyClaims').and.returnValue(of(mockClaims));

    component.ngOnInit();

    expect(claimService.getMyClaims).toHaveBeenCalled();
    expect(component.claims).toEqual(mockClaims);
    expect(component.loading).toBeFalse();
  });

  it('should return correct badge class for status', () => {
    expect(component.getStatusBadgeClass('Submitted')).toBe('bg-blue-500');
    expect(component.getStatusBadgeClass('Settled')).toBe('bg-green-500');
    expect(component.getStatusBadgeClass('Unknown')).toBe('bg-gray-500');
  });

  it('should handle error during claims fetch', () => {
    spyOn(console, 'error');
    spyOn(claimService, 'getMyClaims').and.returnValue(throwError(() => new Error('API Error')));

    component.loadClaims();

    expect(component.loading).toBeFalse();
    expect(console.error).toHaveBeenCalled();
  });
});
