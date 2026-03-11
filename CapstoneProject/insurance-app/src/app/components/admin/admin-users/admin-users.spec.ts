import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AdminUsers } from './admin-users';
import { UserService } from '../../../services/user-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';

describe('AdminUsers', () => {
  let component: AdminUsers;
  let fixture: ComponentFixture<AdminUsers>;
  let userService: UserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminUsers, FormsModule],
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUsers);
    component = fixture.componentInstance;
    userService = TestBed.inject(UserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should switch tab and load customers', () => {
    const mockCustomers = [{ id: 1, name: 'Customer A' }];
    spyOn(userService, 'getCustomers').and.returnValue(of(mockCustomers));

    component.switchTab('Customer');

    expect(component.activeTab).toBe('Customer');
    expect(userService.getCustomers).toHaveBeenCalled();
    expect(component.users).toEqual(mockCustomers);
  });

  it('should switch tab and load agents', () => {
    const mockAgents = [{ id: 2, name: 'Agent B' }];
    spyOn(userService, 'getAgents').and.returnValue(of(mockAgents));

    component.switchTab('Agent');

    expect(component.activeTab).toBe('Agent');
    expect(userService.getAgents).toHaveBeenCalled();
    expect(component.users).toEqual(mockAgents);
  });

  it('should call createAgent when submitting in Agent tab', () => {
    spyOn(userService, 'createAgent').and.returnValue(of({}));
    spyOn(component, 'switchTab');
    spyOn(window, 'alert');

    component.activeTab = 'Agent';
    const expectedUser = { name: 'New Agent', email: 'a@a.com', password: '123', phone: '123' };
    component.newUser = { ...expectedUser };
    component.onSubmit();

    expect(userService.createAgent).toHaveBeenCalledWith(expectedUser);
    expect(window.alert).toHaveBeenCalledWith('Agent created successfully!');
    expect(component.showModal).toBeFalse();
  });


});
