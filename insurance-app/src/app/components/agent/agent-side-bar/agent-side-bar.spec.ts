import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AgentSideBar } from './agent-side-bar';

describe('AgentSideBar', () => {
  let component: AgentSideBar;
  let fixture: ComponentFixture<AgentSideBar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AgentSideBar]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AgentSideBar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
