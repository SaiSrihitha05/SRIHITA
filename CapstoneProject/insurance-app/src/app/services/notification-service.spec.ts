import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { NotificationService } from './notification-service';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        NotificationService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should update unreadCount signal when fetching notifications', () => {
    const mockNotifications = [
      { id: 1, isRead: false },
      { id: 2, isRead: true },
      { id: 3, isRead: false }
    ];

    service.getMyNotifications().subscribe();

    const req = httpMock.expectOne('https://localhost:7027/api/Notifications');
    req.flush(mockNotifications);

    expect(service.unreadCount()).toBe(2);
  });

  it('should decrement unreadCount when marking as read', () => {
    service.unreadCount.set(5);

    service.markAsRead(1).subscribe();

    const req = httpMock.expectOne('https://localhost:7027/api/Notifications/mark-read/1');
    req.flush({});

    expect(service.unreadCount()).toBe(4);
  });


});
