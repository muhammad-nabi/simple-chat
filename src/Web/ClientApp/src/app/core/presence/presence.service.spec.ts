import { TestBed, fakeAsync, tick, discardPeriodicTasks } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PresenceService } from './presence.service';
import { AuthService } from '../services/auth.service';
import { BehaviorSubject } from 'rxjs';

describe('PresenceService', () => {
  let service: PresenceService;
  let httpMock: HttpTestingController;
  let isAuthenticated$: BehaviorSubject<boolean>;

  beforeEach(() => {
    isAuthenticated$ = new BehaviorSubject<boolean>(false);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        PresenceService,
        {
          provide: AuthService,
          useValue: {
            isAuthenticated$,
            accessToken: 'mock-token',
          },
        },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    service = TestBed.inject(PresenceService);
  });

  afterEach(() => {
    // Stop the service to clear intervals before verifying no outstanding requests
    isAuthenticated$.next(false);
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start heartbeat and polling when authenticated', fakeAsync(() => {
    isAuthenticated$.next(true);
    tick(0);

    // Should have sent initial heartbeat and poll
    const heartbeatReq = httpMock.expectOne('/api/presence/heartbeat');
    expect(heartbeatReq.request.method).toBe('POST');
    expect(heartbeatReq.request.body.status).toBe('Online');
    heartbeatReq.flush({});

    const pollReq = httpMock.expectOne('/api/presence/online');
    expect(pollReq.request.method).toBe('GET');
    pollReq.flush([]);

    // Stop before fakeAsync ends to discard timers
    isAuthenticated$.next(false);
    discardPeriodicTasks();
  }));

  it('should stop intervals on logout', fakeAsync(() => {
    isAuthenticated$.next(true);
    tick(0);

    // Flush initial requests
    httpMock.expectOne('/api/presence/heartbeat').flush({});
    httpMock.expectOne('/api/presence/online').flush([]);

    // Log out
    isAuthenticated$.next(false);
    tick(0);

    // Wait for next heartbeat interval — should NOT fire
    tick(60000);
    httpMock.expectNone('/api/presence/heartbeat');

    discardPeriodicTasks();
  }));

  it('should update onlineUsers$ when poll returns users', fakeAsync(() => {
    const mockUsers = [
      { userId: 'u1', displayName: 'Jane', status: 'Online' as const },
    ];

    isAuthenticated$.next(true);
    tick(0);

    httpMock.expectOne('/api/presence/heartbeat').flush({});
    httpMock.expectOne('/api/presence/online').flush(mockUsers);
    tick(0);

    let users: unknown[] = [];
    service.onlineUsers$.subscribe(u => users = u);

    expect(users).toEqual(mockUsers);

    isAuthenticated$.next(false);
    discardPeriodicTasks();
  }));

  it('should clear online users on stop', fakeAsync(() => {
    isAuthenticated$.next(true);
    tick(0);

    httpMock.expectOne('/api/presence/heartbeat').flush({});
    httpMock.expectOne('/api/presence/online').flush([
      { userId: 'u1', displayName: 'Jane', status: 'Online' },
    ]);
    tick(0);

    let users: unknown[] = [];
    service.onlineUsers$.subscribe(u => users = u);

    isAuthenticated$.next(false);
    tick(0);

    expect(users).toEqual([]);

    discardPeriodicTasks();
  }));
});
