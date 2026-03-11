import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ClaimsOfficerProfile } from './profile';
import { UserService } from '../../../services/user-service';
import { of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';

describe('ClaimsOfficerProfile', () => {
  let component: ClaimsOfficerProfile;
  let fixture: ComponentFixture<ClaimsOfficerProfile>;
  let userService: UserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimsOfficerProfile, FormsModule],
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimsOfficerProfile);
    component = fixture.componentInstance;
    userService = TestBed.inject(UserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load profile on init', () => {
    const mockProfile = { name: 'Officer A', email: 'a@a.com', phone: '123' };
    spyOn(userService, 'getProfile').and.returnValue(of(mockProfile));

    component.ngOnInit();

    expect(userService.getProfile).toHaveBeenCalled();
    expect(component.profile).toEqual(mockProfile);
    expect(component.loading).toBeFalse();
  });



  it('should show alert on update failure', () => {
    spyOn(userService, 'updateProfile').and.returnValue(throwError(() => ({ error: { message: 'Failed' } })));
    spyOn(window, 'alert');

    component.onUpdate();

    expect(window.alert).toHaveBeenCalledWith('Update failed: Failed');
  });
});
