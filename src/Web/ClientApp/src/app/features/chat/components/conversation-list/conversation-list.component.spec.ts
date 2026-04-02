import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject } from 'rxjs';
import { ConversationListComponent } from './conversation-list.component';
import { ConversationService } from '../../services/conversation.service';
import { Conversation } from '../../models/conversation.model';

describe('ConversationListComponent', () => {
  let component: ConversationListComponent;
  let fixture: ComponentFixture<ConversationListComponent>;
  let conversationsSubject: BehaviorSubject<Conversation[]>;
  let selectedSubject: BehaviorSubject<Conversation | null>;
  let loadingSubject: BehaviorSubject<boolean>;
  let mockConversationService: Partial<ConversationService>;

  const mockConversations: Conversation[] = [
    {
      id: 1,
      type: 'Private',
      name: null,
      lastMessagePreview: 'Hello!',
      lastMessageAt: '2026-04-01T12:00:00Z',
      otherParticipants: [{ userId: 'user-2', displayName: 'Bob' }],
      unreadCount: 3,
    },
    {
      id: 2,
      type: 'Private',
      name: null,
      lastMessagePreview: 'Hi',
      lastMessageAt: '2026-04-02T12:00:00Z',
      otherParticipants: [{ userId: 'user-3', displayName: 'Charlie' }],
      unreadCount: 0,
    },
  ];

  beforeEach(async () => {
    conversationsSubject = new BehaviorSubject<Conversation[]>([]);
    selectedSubject = new BehaviorSubject<Conversation | null>(null);
    loadingSubject = new BehaviorSubject<boolean>(false);

    mockConversationService = {
      conversations: conversationsSubject.asObservable(),
      selectedConversation: selectedSubject.asObservable(),
      loading: loadingSubject.asObservable(),
      error: new BehaviorSubject<string | null>(null).asObservable(),
      loadConversations: jest.fn(),
      selectConversation: jest.fn(),
      getDisplayName: jest.fn((c: Conversation) =>
        c.type === 'Private' ? c.otherParticipants[0]?.displayName ?? 'Unknown' : c.name ?? 'Conversation'
      ),
    };

    await TestBed.configureTestingModule({
      imports: [ConversationListComponent],
      providers: [
        { provide: ConversationService, useValue: mockConversationService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ConversationListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should call loadConversations on init', () => {
    fixture.detectChanges();
    expect(mockConversationService.loadConversations).toHaveBeenCalled();
  });

  it('should show skeleton loading state', () => {
    loadingSubject.next(true);
    fixture.detectChanges();

    const skeletons = fixture.nativeElement.querySelectorAll('.skeleton-item');
    expect(skeletons.length).toBe(5);
  });

  it('should hide skeleton when not loading', () => {
    loadingSubject.next(false);
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const skeletons = fixture.nativeElement.querySelectorAll('.skeleton-item');
    expect(skeletons.length).toBe(0);
  });

  it('should render conversation list items', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.conversation-item');
    expect(items.length).toBe(2);
  });

  it('should display conversation names', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const names = fixture.nativeElement.querySelectorAll('.conversation-name');
    expect(names[0].textContent.trim()).toBe('Bob');
    expect(names[1].textContent.trim()).toBe('Charlie');
  });

  it('should display last message preview', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const previews = fixture.nativeElement.querySelectorAll('.conversation-preview');
    expect(previews[0].textContent.trim()).toBe('Hello!');
  });

  it('should show unread badge when unreadCount > 0', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const badges = fixture.nativeElement.querySelectorAll('app-unread-badge');
    expect(badges.length).toBe(1); // Only conv with unreadCount=3
  });

  it('should have role=listbox on container', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const list = fixture.nativeElement.querySelector('[role="listbox"]');
    expect(list).toBeTruthy();
  });

  it('should have role=option on items', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('[role="option"]');
    expect(items.length).toBe(2);
  });

  it('should mark active conversation with aria-selected', () => {
    conversationsSubject.next(mockConversations);
    selectedSubject.next(mockConversations[0]);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('[role="option"]');
    expect(items[0].getAttribute('aria-selected')).toBe('true');
    expect(items[1].getAttribute('aria-selected')).toBe('false');
  });

  it('should apply active class to selected conversation', () => {
    conversationsSubject.next(mockConversations);
    selectedSubject.next(mockConversations[0]);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.conversation-item');
    expect(items[0].classList.contains('active')).toBe(true);
    expect(items[1].classList.contains('active')).toBe(false);
  });

  it('should apply unread class when unreadCount > 0', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.conversation-item');
    expect(items[0].classList.contains('unread')).toBe(true);
    expect(items[1].classList.contains('unread')).toBe(false);
  });

  it('should emit conversationSelected on click', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const emittedValues: Conversation[] = [];
    component.conversationSelected.subscribe((c: Conversation) => emittedValues.push(c));

    const items = fixture.nativeElement.querySelectorAll('.conversation-item');
    items[0].click();

    expect(mockConversationService.selectConversation).toHaveBeenCalledWith(mockConversations[0]);
    expect(emittedValues.length).toBe(1);
    expect(emittedValues[0].id).toBe(1);
  });

  it('should show empty state when no conversations', () => {
    conversationsSubject.next([]);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No conversations yet');
  });

  it('should render avatar components', () => {
    conversationsSubject.next(mockConversations);
    fixture.detectChanges();

    const avatars = fixture.nativeElement.querySelectorAll('app-avatar');
    expect(avatars.length).toBe(2);
  });

  it('should have skeleton aria attributes for accessibility', () => {
    loadingSubject.next(true);
    fixture.detectChanges();

    const skeleton = fixture.nativeElement.querySelector('.skeleton-list');
    expect(skeleton.getAttribute('aria-busy')).toBe('true');

    const items = fixture.nativeElement.querySelectorAll('.skeleton-item');
    expect(items[0].getAttribute('aria-hidden')).toBe('true');
  });
});
