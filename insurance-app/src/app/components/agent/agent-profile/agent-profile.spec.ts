import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AgentProfile } from './agent-profile';

describe('AgentProfile', () => {
  let component: AgentProfile;
  let fixture: ComponentFixture<AgentProfile>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AgentProfile]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AgentProfile);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
