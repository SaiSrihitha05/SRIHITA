import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClaimsOfficerProfile } from './Profile';

describe('ClaimsOfficerProfile', () => {
  let component: ClaimsOfficerProfile;
  let fixture: ComponentFixture<ClaimsOfficerProfile>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimsOfficerProfile]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClaimsOfficerProfile);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
