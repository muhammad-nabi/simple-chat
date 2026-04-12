import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { BrowseGroupsComponent } from './browse-groups.component';
import { ConversationService, BrowseGroupDto } from '../../services/conversation.service';
import { Conversation } from '../../models/conversation.model';

describe('BrowseGroupsComponent', () => {
  let component: BrowseGroupsComponent;
  let fixture: ComponentFixture<BrowseGroupsComponent>;
  let mockConversationService: Partial<ConversationService>;
  let conversationCreatedSubject: Subject<number>;
  let errorSubject: BehaviorSubject<string | null>;

  const mockGroups: BrowseGroupDto[] = [
    {
      id: 1,
      name: 'Engineering',
      participantCount: 5,
      lastMessagePreview: 'Latest standup notes',
      lastMessageAt: '2026-04-03T10:00:00Z',
    },
    {
      id: 2,
      name: 'Design Team',
      participantCount: 3,
      lastMessagePreview: null,
      lastMessageAt: null,
    },
  ];

  beforeEach(async () => {
    conversationCreatedSubject = new Subject<number>();
    errorSubject = new BehaviorSubject<string | null>(null);

    mockConversationService = {
      loadBrowseGroups: jest.fn().mockReturnValue(of(mockGroups)),
      joinGroup: jest.fn(),
      conversationCreated: conversationCreatedSubject.asObservable(),
      conversations: new BehaviorSubject<Conversation[]>([]).asObservable(),
      selectedConversation: new BehaviorSubject<Conversation | null>(null).asObservable(),
      loading: new BehaviorSubject<boolean>(false).asObservable(),
      error: errorSubject.asObservable(),
    };

    await TestBed.configureTestingModule({
      imports: [BrowseGroupsComponent],
      providers: [
        { provide: ConversationService, useValue: mockConversationService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BrowseGroupsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load groups on init', () => {
    expect(mockConversationService.loadBrowseGroups).toHaveBeenCalled();
  });

  it('should render group list', () => {
    const items = fixture.nativeElement.querySelectorAll('.group-item');
    expect(items.length).toBe(2);
  });

  it('should display group name', () => {
    const names = fixture.nativeElement.querySelectorAll('.group-name');
    expect(names[0].textContent.trim()).toBe('Engineering');
    expect(names[1].textContent.trim()).toBe('Design Team');
  });

  it('should display participant count', () => {
    const members = fixture.nativeElement.querySelectorAll('.group-members');
    expect(members[0].textContent.trim()).toBe('5 members');
    expect(members[1].textContent.trim()).toBe('3 members');
  });

  it('should display last message preview when available', () => {
    const previews = fixture.nativeElement.querySelectorAll('.group-preview');
    expect(previews.length).toBe(1);
    expect(previews[0].textContent.trim()).toBe('Latest standup notes');
  });

  it('should show join buttons', () => {
    const buttons = fixture.nativeElement.querySelectorAll('.join-button');
    expect(buttons.length).toBe(2);
    expect(buttons[0].textContent.trim()).toBe('Join');
  });

  it('should call joinGroup when join button clicked', () => {
    const buttons = fixture.nativeElement.querySelectorAll('.join-button');
    buttons[0].click();
    expect(mockConversationService.joinGroup).toHaveBeenCalledWith(1);
  });

  it('should show empty state when no groups available', () => {
    (mockConversationService.loadBrowseGroups as jest.Mock).mockReturnValue(of([]));
    component.loadGroups();
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No groups to join right now');
  });

  it('should emit closed when back button clicked', () => {
    const closedSpy = jest.fn();
    component.closed.subscribe(closedSpy);

    const backButton = fixture.nativeElement.querySelector('.back-button');
    backButton.click();

    expect(closedSpy).toHaveBeenCalled();
  });

  it('should emit groupJoined after successful join', () => {
    const joinedSpy = jest.fn();
    component.groupJoined.subscribe(joinedSpy);

    component.joinGroup(mockGroups[0]);
    conversationCreatedSubject.next(1);

    expect(joinedSpy).toHaveBeenCalledWith(1);
  });

  it('should reset joining state on error', () => {
    component.joinGroup(mockGroups[0]);

    let joiningId: number | null = undefined as unknown as number | null;
    component.joiningId$.subscribe(id => joiningId = id);
    expect(joiningId).toBe(1);

    // Simulate error from service
    errorSubject.next('Failed to join group');

    expect(joiningId).toBeNull();
  });

  it('should not emit groupJoined on error', () => {
    const joinedSpy = jest.fn();
    component.groupJoined.subscribe(joinedSpy);

    component.joinGroup(mockGroups[0]);
    errorSubject.next('Failed to join group');

    expect(joinedSpy).not.toHaveBeenCalled();
  });

  it('should show loading skeleton while loading', () => {
    (mockConversationService.loadBrowseGroups as jest.Mock).mockReturnValue(
      new BehaviorSubject<BrowseGroupDto[]>([]).asObservable()
    );
    component['_loading$'].next(true);
    fixture.detectChanges();

    const skeletonItems = fixture.nativeElement.querySelectorAll('.skeleton-item');
    expect(skeletonItems.length).toBe(3);
  });
});
