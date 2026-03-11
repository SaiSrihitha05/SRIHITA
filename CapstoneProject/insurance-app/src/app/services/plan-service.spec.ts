import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PlanService } from './plan-service';

describe('PlanService', () => {
  let service: PlanService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PlanService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(PlanService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch all plans', () => {
    const mockPlans = [{ id: 1, planName: 'Whole Life' }];

    service.getPlans().subscribe(plans => {
      expect(plans).toEqual(mockPlans);
    });

    const req = httpMock.expectOne('https://localhost:7027/api/Plans');
    expect(req.request.method).toBe('GET');
    req.flush(mockPlans);
  });

  it('should apply filters to getFilteredPlans', () => {
    const filter = { planType: 'Life', age: 30 };
    const mockPlans = [{ id: 1, type: 'Life' }];

    service.getFilteredPlans(filter).subscribe(plans => {
      expect(plans).toEqual(mockPlans);
    });

    const req = httpMock.expectOne(request =>
      request.url === 'https://localhost:7027/api/Plans/filter' &&
      request.params.get('planType') === 'Life' &&
      request.params.get('age') === '30'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockPlans);
  });
});
