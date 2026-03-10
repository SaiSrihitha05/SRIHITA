// src/app/app.spec.ts

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
  }) as unknown as MediaQueryList,
});

import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';

describe('App Component', () => {

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient()
      ]
    }).compileComponents();

    // ✅ Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ── Test 1: Component Creation ──────────────────────────────
  it('should create the app component', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  // ── Test 2: Default title signal ────────────────────────────
  it('should have title set to insurance-app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app['title']()).toBe('insurance-app');
  });

  // ── Test 3: isLoggedIn false when no token ───────────────────
  it('should return false for isLoggedIn when no token in localStorage', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    localStorage.removeItem('token');
    expect(app.isLoggedIn).toBeFalse();
  });

  // ── Test 4: isLoggedIn true when token exists ────────────────
  it('should return true for isLoggedIn when token exists in localStorage', () => {
    localStorage.setItem('token', 'mock-jwt-token');
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app.isLoggedIn).toBeTrue();
  });

  // ── Test 5: Router outlet rendered ──────────────────────────
  it('should render router-outlet', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });

  // ── Test 6: App renders without errors ───────────────────────
  it('should render without throwing errors', () => {
    expect(() => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
    }).not.toThrow();
  });

});