// 1. Mock matchMedia to prevent ThemeService/Layout crashes in Vitest
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
import { ClaimsOfficerSideBar } from './claims-officer-side-bar'; // Ensure this is the correct component
import { provideRouter } from '@angular/router';

describe('CustomerSideBar', () => {
  let component: ClaimsOfficerSideBar;
  let fixture: ComponentFixture<ClaimsOfficerSideBar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimsOfficerSideBar], // Import the CustomerSideBar standalone component
      providers: [
        provideRouter([]) // Provides context for RouterLink and RouterLinkActive
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClaimsOfficerSideBar);
    component = fixture.componentInstance;
    fixture.detectChanges(); // Triggers life-cycle hooks
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});