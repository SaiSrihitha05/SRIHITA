import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminPlans } from './admin-plans';

describe('AdminPlans', () => {
  let component: AdminPlans;
  let fixture: ComponentFixture<AdminPlans>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPlans]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminPlans);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
