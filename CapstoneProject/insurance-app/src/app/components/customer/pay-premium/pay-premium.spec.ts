import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PayPremium } from './pay-premium';

describe('PayPremium', () => {
  let component: PayPremium;
  let fixture: ComponentFixture<PayPremium>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PayPremium]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PayPremium);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
