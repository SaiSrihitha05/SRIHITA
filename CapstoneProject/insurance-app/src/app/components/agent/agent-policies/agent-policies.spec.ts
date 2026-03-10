import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AgentPolicies } from './agent-policies';

describe('AgentPolicies', () => {
  let component: AgentPolicies;
  let fixture: ComponentFixture<AgentPolicies>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AgentPolicies]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AgentPolicies);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
