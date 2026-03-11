import { TestBed, ComponentFixture } from '@angular/core/testing';
import { NotificationCenter } from './notification-center';
import { NotificationService } from '../../../services/notification-service';
import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

describe('NotificationCenter', () => {
  let component: NotificationCenter;
  let fixture: ComponentFixture<NotificationCenter>;
  let notifyService: NotificationService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationCenter],
      providers: [
        NotificationService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationCenter);
    component = fixture.componentInstance;
    notifyService = TestBed.inject(NotificationService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load notifications on init', () => {
    const mockNotifications = [{ id: 1, type: 'Notice' }];
    spyOn(notifyService, 'getMyNotifications').and.returnValue(of(mockNotifications));

    component.ngOnInit();

    expect(notifyService.getMyNotifications).toHaveBeenCalled();
    expect(component.notifications).toEqual(mockNotifications);
  });

});
