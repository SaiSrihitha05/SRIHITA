import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Profile } from './profile';
import { UserService } from '../../../services/user-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';

describe('Customer Profile', () => {
  let component: Profile;
  let fixture: ComponentFixture<Profile>;
  let userService: UserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Profile, FormsModule],
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Profile);
    component = fixture.componentInstance;
    userService = TestBed.inject(UserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should save profile and show success alert', () => {
    const updatedProfile = { name: 'Updated' };
    spyOn(userService, 'updateProfile').and.returnValue(of(updatedProfile));
    spyOn(window, 'alert');

    component.editData = updatedProfile;
    component.saveProfile();

    expect(userService.updateProfile).toHaveBeenCalledWith(updatedProfile);
    expect(component.userProfile).toEqual(updatedProfile);
    expect(window.alert).toHaveBeenCalledWith('Profile updated successfully!');
  });

});
