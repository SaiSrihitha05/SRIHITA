import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExplorePlans } from './explore-plans';

describe('ExplorePlans', () => {
  let component: ExplorePlans;
  let fixture: ComponentFixture<ExplorePlans>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExplorePlans]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExplorePlans);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
