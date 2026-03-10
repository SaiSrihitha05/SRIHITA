import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminPolicies } from './admin-policies';

describe('AdminPolicies', () => {
  let component: AdminPolicies;
  let fixture: ComponentFixture<AdminPolicies>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPolicies]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminPolicies);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
