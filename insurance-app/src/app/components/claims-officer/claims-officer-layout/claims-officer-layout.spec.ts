// 1. Mock matchMedia to prevent ThemeService from crashing in Vitest
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClaimsOfficerLayout } from './claims-officer-layout';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('ClaimsOfficerLayout', () => {
  let component: ClaimsOfficerLayout;
  let fixture: ComponentFixture<ClaimsOfficerLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      // Ensure ClaimsOfficerLayout is in imports since it is a standalone component
      imports: [ClaimsOfficerLayout],
      providers: [
        provideRouter([]), // Fixes "No provider found for ActivatedRoute"
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClaimsOfficerLayout);
    component = fixture.componentInstance;
    fixture.detectChanges(); // Use detectChanges instead of whenStable for initial creation
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});