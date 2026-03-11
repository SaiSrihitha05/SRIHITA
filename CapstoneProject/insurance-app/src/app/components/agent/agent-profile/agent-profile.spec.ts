import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AgentProfile } from './agent-profile';
import { UserService } from '../../../services/user-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';

describe('AgentProfile', () => {
  let component: AgentProfile;
  let fixture: ComponentFixture<AgentProfile>;
  let userService: UserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AgentProfile, FormsModule],
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AgentProfile);
    component = fixture.componentInstance;
    userService = TestBed.inject(UserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should update profile and show success alert', () => {
    const mockProfileData = { name: 'Agent Updated', phone: '456', isActive: true };
    component.profile = mockProfileData;

    spyOn(userService, 'updateProfile').and.returnValue(of({ message: 'Success' }));
    spyOn(window, 'alert');
    spyOn(component, 'loadProfile');

    component.onUpdate();

    expect(userService.updateProfile).toHaveBeenCalledWith({
      name: 'Agent Updated',
      phone: '456',
      isActive: true
    });
    expect(window.alert).toHaveBeenCalledWith('Success');
    expect(component.isEditing).toBeFalse();
    expect(component.loadProfile).toHaveBeenCalled();
  });

});
