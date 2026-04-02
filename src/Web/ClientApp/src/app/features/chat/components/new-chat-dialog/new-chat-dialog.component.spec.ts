import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject } from 'rxjs';
import { NewChatDialogComponent } from './new-chat-dialog.component';
import { UserService } from '../../services/user.service';
import { TeamMember } from '../../models/user.model';

describe('NewChatDialogComponent', () => {
  let component: NewChatDialogComponent;
  let fixture: ComponentFixture<NewChatDialogComponent>;
  let teamMembersSubject: BehaviorSubject<TeamMember[]>;
  let loadingSubject: BehaviorSubject<boolean>;
  let errorSubject: BehaviorSubject<string | null>;
  let mockUserService: Partial<UserService>;

  const mockMembers: TeamMember[] = [
    { userId: 'user-2', displayName: 'Alice Johnson' },
    { userId: 'user-3', displayName: 'Bob Smith' },
    { userId: 'user-4', displayName: 'Charlie Brown' },
  ];

  beforeEach(async () => {
    teamMembersSubject = new BehaviorSubject<TeamMember[]>([]);
    loadingSubject = new BehaviorSubject<boolean>(false);
    errorSubject = new BehaviorSubject<string | null>(null);

    mockUserService = {
      teamMembers$: teamMembersSubject.asObservable(),
      loading$: loadingSubject.asObservable(),
      error$: errorSubject.asObservable(),
      loadTeamMembers: jest.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [NewChatDialogComponent],
      providers: [
        { provide: UserService, useValue: mockUserService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NewChatDialogComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should call loadTeamMembers on init', () => {
    fixture.detectChanges();
    expect(mockUserService.loadTeamMembers).toHaveBeenCalled();
  });

  it('should render dialog with New Chat title', () => {
    fixture.detectChanges();
    const title = fixture.nativeElement.querySelector('.dialog-header h3');
    expect(title.textContent).toBe('New Chat');
  });

  it('should render filter input with aria-label', () => {
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('.filter-input');
    expect(input).toBeTruthy();
    expect(input.getAttribute('aria-label')).toBe('Filter team members');
  });

  it('should show skeleton loading state', () => {
    loadingSubject.next(true);
    fixture.detectChanges();

    const skeletons = fixture.nativeElement.querySelectorAll('.skeleton-item');
    expect(skeletons.length).toBe(4);
  });

  it('should render team member list', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.member-item');
    expect(items.length).toBe(3);
  });

  it('should display member names', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const names = fixture.nativeElement.querySelectorAll('.member-name');
    expect(names[0].textContent.trim()).toBe('Alice Johnson');
    expect(names[1].textContent.trim()).toBe('Bob Smith');
  });

  it('should render avatar components', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const avatars = fixture.nativeElement.querySelectorAll('app-avatar');
    expect(avatars.length).toBe(3);
  });

  it('should filter members by name', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    component.filterText = 'alice';
    component.onFilterChange();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.member-item');
    expect(items.length).toBe(1);
    expect(fixture.nativeElement.querySelector('.member-name').textContent.trim()).toBe('Alice Johnson');
  });

  it('should show empty filter message when no match', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    component.filterText = 'zzzzz';
    component.onFilterChange();
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.empty-filter');
    expect(empty).toBeTruthy();
    expect(empty.textContent).toContain('No members match your search');
  });

  it('should have role=listbox on member list', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const list = fixture.nativeElement.querySelector('[role="listbox"]');
    expect(list).toBeTruthy();
  });

  it('should have role=option on member items', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('[role="option"]');
    expect(items.length).toBe(3);
  });

  it('should emit userSelected when member is clicked', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const emitted: TeamMember[] = [];
    component.userSelected.subscribe((m: TeamMember) => emitted.push(m));

    const items = fixture.nativeElement.querySelectorAll('.member-item');
    items[0].click();

    expect(emitted.length).toBe(1);
    expect(emitted[0].userId).toBe('user-2');
  });

  it('should emit closed when close button clicked', () => {
    fixture.detectChanges();

    let closedEmitted = false;
    component.closed.subscribe(() => closedEmitted = true);

    const closeBtn = fixture.nativeElement.querySelector('.close-button');
    closeBtn.click();

    expect(closedEmitted).toBe(true);
  });

  it('should emit closed when backdrop clicked', () => {
    fixture.detectChanges();

    let closedEmitted = false;
    component.closed.subscribe(() => closedEmitted = true);

    const backdrop = fixture.nativeElement.querySelector('.dialog-backdrop');
    backdrop.click();

    expect(closedEmitted).toBe(true);
  });

  it('should not close when dialog panel clicked', () => {
    fixture.detectChanges();

    let closedEmitted = false;
    component.closed.subscribe(() => closedEmitted = true);

    const panel = fixture.nativeElement.querySelector('.dialog-panel');
    panel.click();

    expect(closedEmitted).toBe(false);
  });

  it('should close on Escape key', () => {
    fixture.detectChanges();

    let closedEmitted = false;
    component.closed.subscribe(() => closedEmitted = true);

    component.onKeyDown(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(closedEmitted).toBe(true);
  });

  it('should navigate with ArrowDown key', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    component.onKeyDown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    expect(component.activeIndex).toBe(0);

    component.onKeyDown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    expect(component.activeIndex).toBe(1);
  });

  it('should navigate with ArrowUp key', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    component.activeIndex = 2;
    component.onKeyDown(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    expect(component.activeIndex).toBe(1);
  });

  it('should not go below 0 with ArrowUp', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    component.activeIndex = 0;
    component.onKeyDown(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    expect(component.activeIndex).toBe(0);
  });

  it('should select member with Enter key when active', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const emitted: TeamMember[] = [];
    component.userSelected.subscribe((m: TeamMember) => emitted.push(m));

    component.activeIndex = 1;
    component.onKeyDown(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(emitted.length).toBe(1);
    expect(emitted[0].userId).toBe('user-3');
  });

  it('should not select on Enter when no active index', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    const emitted: TeamMember[] = [];
    component.userSelected.subscribe((m: TeamMember) => emitted.push(m));

    component.onKeyDown(new KeyboardEvent('keydown', { key: 'Enter' }));
    expect(emitted.length).toBe(0);
  });

  it('should reset activeIndex on filter change', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    component.activeIndex = 2;
    component.filterText = 'alice';
    component.onFilterChange();

    expect(component.activeIndex).toBe(-1);
  });

  it('should show error state', () => {
    errorSubject.next('Failed to load team members');
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-state');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to load team members');
  });

  it('should have close button with aria-label', () => {
    fixture.detectChanges();

    const closeBtn = fixture.nativeElement.querySelector('.close-button');
    expect(closeBtn.getAttribute('aria-label')).toBe('Close');
  });

  it('should have dialog role and aria-modal', () => {
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('[role="dialog"]');
    expect(panel).toBeTruthy();
    expect(panel.getAttribute('aria-modal')).toBe('true');
  });

  it('should not exceed list bounds with ArrowDown', () => {
    teamMembersSubject.next(mockMembers);
    fixture.detectChanges();

    component.activeIndex = 2;
    component.onKeyDown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    expect(component.activeIndex).toBe(2); // stays at last
  });
});
