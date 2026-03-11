import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard } from './auth-guard';

describe('authGuard', () => {
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    });
    router = TestBed.inject(Router);
    localStorage.clear();
  });

  const executeGuard = (routeData: any = {}) => {
    const route = { data: routeData } as any;
    const state = {} as any;
    return TestBed.runInInjectionContext(() => authGuard(route, state));
  };

  it('should redirect to login if no token exists', () => {
    const result = executeGuard();
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should allow access if token exists and no role requirement', () => {
    localStorage.setItem('token', 'some-token');
    const result = executeGuard();
    expect(result).toBeTrue();
  });

  it('should allow access if token exists and role matches', () => {
    localStorage.setItem('token', 'some-token');
    localStorage.setItem('role', 'Admin');
    const result = executeGuard({ role: 'Admin' });
    expect(result).toBeTrue();
  });


});
