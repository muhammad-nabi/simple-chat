import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, Subject } from 'rxjs';
import { ChatWindowComponent } from './chat-window.component';
import { MessageService } from '../../services/message.service';
import { ConversationService } from '../../services/conversation.service';
import { AuthService, CurrentUser } from '../../../../core/services/auth.service';
import { SignalRService } from '../../../../core/signalr/signalr.service';
import { Message } from '../../models/message.model';
import { Conversation } from '../../models/conversation.model';

describe('ChatWindowComponent', () => {
  let component: ChatWindowComponent;
  let fixture: ComponentFixture<ChatWindowComponent>;
  let messagesSubject: BehaviorSubject<Message[]>;
  let loadingSubject: BehaviorSubject<boolean>;
  let hasMoreSubject: BehaviorSubject<boolean>;
  let selectedConvSubject: BehaviorSubject<Conversation | null>;
  let mockMessageService: Record<string, unknown>;
  let reconnectedSubject: Subject<void>;

  const mockConversation: Conversation = {
    id: 1,
    type: 'Private',
    name: null,
    lastMessagePreview: 'Hello',
    lastMessageAt: '2026-04-01T12:00:00Z',
    otherParticipants: [{ userId: 'user-2', displayName: 'Alice' }],
    unreadCount: 0,
  };

  const mockGroupConversation: Conversation = {
    id: 2,
    type: 'Group',
    name: 'Engineering Team',
    lastMessagePreview: 'Hello team',
    lastMessageAt: '2026-04-01T12:00:00Z',
    otherParticipants: [
      { userId: 'user-2', displayName: 'Alice' },
      { userId: 'user-3', displayName: 'Bob' },
    ],
    unreadCount: 0,
  };

  const mockMessages: Message[] = [
    { id: 1, conversationId: 1, senderId: 'user-2', senderDisplayName: 'Alice', content: 'Hey', sentAt: '2026-04-01T08:00:00Z', messageType: 'Text' },
    { id: 2, conversationId: 1, senderId: 'user-1', senderDisplayName: 'Me', content: 'Hello!', sentAt: '2026-04-01T08:01:00Z', messageType: 'Text' },
    { id: 3, conversationId: 1, senderId: 'user-2', senderDisplayName: 'Alice', content: 'How are you?', sentAt: '2026-04-01T08:02:00Z', messageType: 'Text' },
  ];

  beforeEach(async () => {
    messagesSubject = new BehaviorSubject<Message[]>([]);
    loadingSubject = new BehaviorSubject<boolean>(false);
    hasMoreSubject = new BehaviorSubject<boolean>(false);
    selectedConvSubject = new BehaviorSubject<Conversation | null>(null);
    reconnectedSubject = new Subject<void>();

    mockMessageService = {
      messages$: messagesSubject.asObservable(),
      loading$: loadingSubject.asObservable(),
      loadingHistory$: new BehaviorSubject<boolean>(false).asObservable(),
      hasMore$: hasMoreSubject.asObservable(),
      error$: new BehaviorSubject<string | null>(null).asObservable(),
      loadMessages: jest.fn(),
      clearMessages: jest.fn(),
      saveScrollPosition: jest.fn(),
      getScrollPosition: jest.fn().mockReturnValue(undefined),
      getOldestMessageId: jest.fn().mockReturnValue(undefined),
      isOwnMessage: jest.fn((senderId: string) => senderId === 'user-1'),
    };

    const mockConversationService = {
      selectedConversation: selectedConvSubject.asObservable(),
    };

    const mockAuthService = {
      currentUser$: new BehaviorSubject<CurrentUser | null>({
        userId: 'user-1',
        displayName: 'Me',
        email: 'me@test.com',
        role: 'Member',
      }),
    };

    const mockSignalRService = {
      connectionState: new BehaviorSubject('Connected').asObservable(),
      reconnected: reconnectedSubject.asObservable(),
      messageReceived: new Subject().asObservable(),
    };

    await TestBed.configureTestingModule({
      imports: [ChatWindowComponent],
      providers: [
        { provide: MessageService, useValue: mockMessageService },
        { provide: ConversationService, useValue: mockConversationService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: SignalRService, useValue: mockSignalRService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show empty state when no conversation is selected', () => {
    const emptyState: HTMLElement | null = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState?.textContent?.trim()).toBe('Select a conversation');
  });

  it('should show skeleton loading when loading initial messages', () => {
    selectedConvSubject.next(mockConversation);
    loadingSubject.next(true);
    fixture.detectChanges();

    const skeleton: HTMLElement | null = fixture.nativeElement.querySelector('.skeleton-container');
    expect(skeleton).toBeTruthy();
    expect(skeleton?.getAttribute('aria-busy')).toBe('true');
  });

  it('should hide skeleton items from screen readers', () => {
    selectedConvSubject.next(mockConversation);
    loadingSubject.next(true);
    fixture.detectChanges();

    const skeletonItems: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.skeleton-bubble');
    skeletonItems.forEach((item: HTMLElement) => {
      expect(item.getAttribute('aria-hidden')).toBe('true');
    });
  });

  it('should render messages when conversation is selected', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next(mockMessages);
    fixture.detectChanges();

    const messageList: HTMLElement | null = fixture.nativeElement.querySelector('.message-list');
    expect(messageList).toBeTruthy();
    expect(messageList?.getAttribute('role')).toBe('log');
    expect(messageList?.getAttribute('aria-live')).toBe('polite');
  });

  it('should render message bubble components', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next(mockMessages);
    fixture.detectChanges();

    const bubbles: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('app-message-bubble');
    expect(bubbles.length).toBe(3);
  });

  it('should load messages when conversation is selected', () => {
    selectedConvSubject.next(mockConversation);
    fixture.detectChanges();

    expect(mockMessageService['loadMessages']).toHaveBeenCalledWith(1);
  });

  it('should clear messages when conversation is deselected', () => {
    selectedConvSubject.next(mockConversation);
    fixture.detectChanges();

    selectedConvSubject.next(null);
    fixture.detectChanges();

    expect(mockMessageService['clearMessages']).toHaveBeenCalled();
  });

  it('should clear messages before loading when switching between conversations', () => {
    const callOrder: string[] = [];
    (mockMessageService['clearMessages'] as jest.Mock).mockImplementation(() => callOrder.push('clear'));
    (mockMessageService['loadMessages'] as jest.Mock).mockImplementation(() => callOrder.push('load'));

    selectedConvSubject.next(mockConversation);
    fixture.detectChanges();
    callOrder.length = 0; // reset after initial load

    selectedConvSubject.next(mockGroupConversation);
    fixture.detectChanges();

    expect(callOrder).toEqual(['clear', 'load']);
    expect(mockMessageService['loadMessages']).toHaveBeenCalledWith(2);
  });

  describe('message grouping', () => {
    it('should not show sender in private conversations', () => {
      component.selectedConversation = mockConversation; // type: 'Private'
      component.messages = mockMessages;
      expect(component.shouldShowSender(0)).toBe(false);
      expect(component.shouldShowSender(1)).toBe(false);
    });

    it('should show sender for first message in group conversation', () => {
      component.selectedConversation = mockGroupConversation;
      component.messages = mockMessages;
      expect(component.shouldShowSender(0)).toBe(true);
    });

    it('should show sender when different from previous in group', () => {
      component.selectedConversation = mockGroupConversation;
      component.messages = mockMessages;
      // message[1] (user-1) differs from message[0] (user-2)
      expect(component.shouldShowSender(1)).toBe(true);
    });

    it('should not flag first message as consecutive', () => {
      component.messages = mockMessages;
      expect(component.isConsecutiveMessage(0)).toBe(false);
    });

    it('should flag consecutive same-sender messages', () => {
      component.selectedConversation = mockGroupConversation;
      const consecutiveMessages: Message[] = [
        { ...mockMessages[0], id: 1, senderId: 'user-2' },
        { ...mockMessages[1], id: 2, senderId: 'user-2' },
      ];
      component.messages = consecutiveMessages;
      expect(component.isConsecutiveMessage(1)).toBe(true);
      expect(component.shouldShowSender(1)).toBe(false);
    });
  });

  describe('system messages', () => {
    const systemMessage: Message = {
      id: 10, conversationId: 1, senderId: 'user-1', senderDisplayName: 'Me',
      content: 'Me created the group', sentAt: '2026-04-01T08:00:00Z', messageType: 'System',
    };

    it('should identify system messages', () => {
      expect(component.isSystemMessage(systemMessage)).toBe(true);
      expect(component.isSystemMessage(mockMessages[0])).toBe(false);
    });

    it('should render system message as centered pill', () => {
      component.selectedConversation = mockGroupConversation;
      messagesSubject.next([systemMessage, ...mockMessages]);
      fixture.detectChanges();

      const pill: HTMLElement | null = fixture.nativeElement.querySelector('.system-message');
      expect(pill).toBeTruthy();
      expect(pill?.textContent?.trim()).toBe('Me created the group');
      expect(pill?.hasAttribute('role')).toBe(false);
    });

    it('should not render system messages as message bubbles', () => {
      component.selectedConversation = mockGroupConversation;
      messagesSubject.next([systemMessage, ...mockMessages]);
      fixture.detectChanges();

      const bubbles: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('app-message-bubble');
      expect(bubbles.length).toBe(3); // only the 3 text messages, not the system message
    });

    it('should break consecutive grouping around system messages', () => {
      component.selectedConversation = mockGroupConversation;
      const messagesWithSystem: Message[] = [
        { ...mockMessages[0], id: 1, senderId: 'user-2', messageType: 'Text' },
        { ...systemMessage, id: 2 },
        { ...mockMessages[2], id: 3, senderId: 'user-2', messageType: 'Text' },
      ];
      component.messages = messagesWithSystem;

      // System message breaks grouping — message after system should show sender
      expect(component.isConsecutiveMessage(2)).toBe(false);
      expect(component.shouldShowSender(2)).toBe(true);
    });

    it('should not show sender for system messages themselves', () => {
      component.selectedConversation = mockGroupConversation;
      component.messages = [systemMessage];
      expect(component.shouldShowSender(0)).toBe(false);
    });
  });

  describe('group conversation detection', () => {
    it('should detect group conversation', () => {
      component.selectedConversation = mockGroupConversation;
      expect(component.isGroupConversation()).toBe(true);
    });

    it('should detect non-group conversation', () => {
      component.selectedConversation = mockConversation;
      expect(component.isGroupConversation()).toBe(false);
    });

    it('should return false when no conversation selected', () => {
      component.selectedConversation = null;
      expect(component.isGroupConversation()).toBe(false);
    });
  });

  describe('own message detection', () => {
    it('should delegate to messageService.isOwnMessage', () => {
      expect(component.isOwnMessage('user-1')).toBe(true);
      expect(component.isOwnMessage('user-2')).toBe(false);
    });
  });

  it('should show loading spinner when loading more history', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next(mockMessages);
    hasMoreSubject.next(true);
    fixture.detectChanges();

    component.loadingHistory = true;
    fixture.detectChanges();

    const spinner: HTMLElement | null = fixture.nativeElement.querySelector('.loading-spinner');
    expect(spinner).toBeTruthy();
  });

  it('should show new messages indicator when hasNewMessages is true', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next(mockMessages);
    fixture.detectChanges();

    component.hasNewMessages = true;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    const indicator: HTMLElement | null = fixture.nativeElement.querySelector('.new-messages-indicator');
    expect(indicator).toBeTruthy();
    expect(indicator?.textContent?.trim()).toContain('New messages');
  });

  it('should hide new messages indicator by default', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next(mockMessages);
    fixture.detectChanges();

    const indicator: HTMLElement | null = fixture.nativeElement.querySelector('.new-messages-indicator');
    expect(indicator).toBeFalsy();
  });

  it('should save scroll position on destroy', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next(mockMessages);
    fixture.detectChanges();

    component.ngOnDestroy();

    expect(mockMessageService['saveScrollPosition']).toHaveBeenCalledWith(1, expect.any(Number));
  });

  it('should track messages by id', () => {
    const msg: Message = mockMessages[0];
    expect(component.trackByMessageId(0, msg)).toBe(1);
  });

  it('should show empty messages state when conversation selected but no messages', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next([]);
    loadingSubject.next(false);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state-messages');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No messages yet');
  });

  describe('reconnection', () => {
    it('should reload messages on reconnect when conversation is active', () => {
      selectedConvSubject.next(mockConversation);
      fixture.detectChanges();
      (mockMessageService['loadMessages'] as jest.Mock).mockClear();

      reconnectedSubject.next();

      expect(mockMessageService['loadMessages']).toHaveBeenCalledWith(1);
    });

    it('should not reload messages on reconnect when no conversation selected', () => {
      selectedConvSubject.next(null);
      fixture.detectChanges();
      (mockMessageService['loadMessages'] as jest.Mock).mockClear();

      reconnectedSubject.next();

      expect(mockMessageService['loadMessages']).not.toHaveBeenCalled();
    });
  });

  it('should not show empty messages state when loading', () => {
    selectedConvSubject.next(mockConversation);
    messagesSubject.next([]);
    loadingSubject.next(true);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state-messages');
    expect(emptyState).toBeFalsy();
  });
});
