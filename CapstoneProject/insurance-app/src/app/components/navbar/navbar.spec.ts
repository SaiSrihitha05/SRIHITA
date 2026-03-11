import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Navbar } from './navbar';
import { NotificationService } from '../../services/notification-service';
import { ThemeToggle } from '../theme-toggle/theme-toggle';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;
  let notifyService: NotificationService;
  let router: Router; // Declare router here

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [
        NotificationService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;
    notifyService = TestBed.inject(NotificationService);
    router = TestBed.inject(Router); // Inject router here
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });



  it('should return correct profile link based on role', () => {
    localStorage.setItem('role', 'Admin');
    expect(component.profileLink).toBe('/admin-dashboard/profile');

    localStorage.setItem('role', 'Agent');
    expect(component.profileLink).toBe('/agent-dashboard/profile');

    localStorage.setItem('role', 'Customer');
    expect(component.profileLink).toBe('/customer-dashboard/profile');
  });

  it('should get correct initial from name or email', () => {
    localStorage.setItem('name', 'Alice');
    expect(component.userInitial).toBe('A');

    localStorage.clear();
    localStorage.setItem('email', 'bob@example.com');
    expect(component.userInitial).toBe('B');

    localStorage.clear();
    expect(component.userInitial).toBe('U');
  });

});
