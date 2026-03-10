import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerSideBar } from './customer-side-bar';

describe('CustomerSideBar', () => {
  let component: CustomerSideBar;
  let fixture: ComponentFixture<CustomerSideBar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerSideBar]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CustomerSideBar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
