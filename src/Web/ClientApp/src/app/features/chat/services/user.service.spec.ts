import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { UserService } from './user.service';
import { TeamMember } from '../models/user.model';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  const mockMembers: TeamMember[] = [
    { userId: 'user-2', displayName: 'Alice' },
    { userId: 'user-3', displayName: 'Bob' },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should load team members from API', () => {
    let result: TeamMember[] = [];
    service.teamMembers$.subscribe(m => result = m);

    service.loadTeamMembers();
    const req = httpMock.expectOne('/api/users/team-members');
    expect(req.request.method).toBe('GET');
    req.flush(mockMembers);

    expect(result.length).toBe(2);
    expect(result[0].displayName).toBe('Alice');
  });

  it('should set loading state during load', () => {
    const loadingStates: boolean[] = [];
    service.loading$.subscribe(l => loadingStates.push(l));

    service.loadTeamMembers();
    const req = httpMock.expectOne('/api/users/team-members');
    req.flush(mockMembers);

    expect(loadingStates).toContain(true);
    expect(loadingStates[loadingStates.length - 1]).toBe(false);
  });

  it('should set error on failure', () => {
    let error: string | null = null;
    service.error$.subscribe(e => error = e);

    service.loadTeamMembers();
    const req = httpMock.expectOne('/api/users/team-members');
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    expect(error).toBe('Failed to load team members');
  });

  it('should skip concurrent load when already loading', () => {
    service.loadTeamMembers();
    service.loadTeamMembers(); // should be skipped

    // Only one request should be pending
    const requests = httpMock.match('/api/users/team-members');
    expect(requests.length).toBe(1);
    requests[0].flush(mockMembers);
  });

  it('should clear error on new load', () => {
    let error: string | null = null;
    service.error$.subscribe(e => error = e);

    // First call fails
    service.loadTeamMembers();
    httpMock.expectOne('/api/users/team-members').flush('Error', { status: 500, statusText: 'Error' });
    expect(error).toBe('Failed to load team members');

    // Second call should clear error
    service.loadTeamMembers();
    expect(error).toBeNull();
    httpMock.expectOne('/api/users/team-members').flush(mockMembers);
  });
});
