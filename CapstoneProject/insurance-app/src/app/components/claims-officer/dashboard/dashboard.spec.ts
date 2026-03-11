import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ClaimsOfficerDashboard } from './dashboard';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CommonModule } from '@angular/common';

describe('Claims Officer Dashboard', () => {
  let component: ClaimsOfficerDashboard;
  let fixture: ComponentFixture<ClaimsOfficerDashboard>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimsOfficerDashboard, CommonModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimsOfficerDashboard);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should fetch dashboard stats on init', () => {
    const mockStats = { assignedClaims: 5, pendingReviews: 2 };

    fixture.detectChanges(); // Trigger ngOnInit

    const req = httpMock.expectOne('https://localhost:7027/api/Dashboard/claims-officer');
    expect(req.request.method).toBe('GET');
    req.flush(mockStats);

    expect(component.stats).toEqual(mockStats);
    expect(component.loading).toBeFalse();
  });

  it('should handle error during stats fetch', () => {
    spyOn(console, 'error');

    fixture.detectChanges(); // Trigger ngOnInit

    const req = httpMock.expectOne('https://localhost:7027/api/Dashboard/claims-officer');
    req.error(new ProgressEvent('Network Error'));

    expect(component.loading).toBeFalse();
    expect(console.error).toHaveBeenCalled();
  });
});
