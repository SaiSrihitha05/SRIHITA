import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AdminProfile } from './admin-profile';
import { UserService } from '../../../services/user-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';

describe('AdminProfile', () => {
  let component: AdminProfile;
  let fixture: ComponentFixture<AdminProfile>;
  let userService: UserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminProfile, FormsModule],
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminProfile);
    component = fixture.componentInstance;
    userService = TestBed.inject(UserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });


  it('should update profile and show success alert', () => {
    const mockProfileData = { name: 'Admin Updated', phone: '456', isActive: true };
    component.profile = mockProfileData;

    spyOn(userService, 'updateProfile').and.returnValue(of({ message: 'Success' }));
    spyOn(window, 'alert');
    spyOn(component, 'loadProfile');

    component.onUpdate();

    expect(userService.updateProfile).toHaveBeenCalledWith({
      name: 'Admin Updated',
      phone: '456',
      isActive: true
    });
    expect(window.alert).toHaveBeenCalledWith('Success');
    expect(component.isEditing).toBeFalse();
    expect(component.loadProfile).toHaveBeenCalled();
  });

});
