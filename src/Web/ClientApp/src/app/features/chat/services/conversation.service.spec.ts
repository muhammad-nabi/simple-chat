import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { BehaviorSubject, Subject } from 'rxjs';
import { ConversationService, CreateConversationResponse } from './conversation.service';
import { Conversation } from '../models/conversation.model';
import { SignalRService } from '../../../core/signalr/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { MessagePayload } from '../../../core/signalr/signalr.events';

describe('ConversationService', () => {
  let service: ConversationService;
  let httpMock: HttpTestingController;
  let messageSubject: Subject<MessagePayload>;
  let reconnectedSubject: Subject<void>;

  const mockConversations: Conversation[] = [
    {
      id: 1,
      type: 'Private',
      name: null,
      lastMessagePreview: 'Hello!',
      lastMessageAt: '2026-04-01T12:00:00Z',
      otherParticipants: [{ userId: 'user-2', displayName: 'Bob' }],
      unreadCount: 2,
    },
    {
      id: 2,
      type: 'Private',
      name: null,
      lastMessagePreview: 'Hi there',
      lastMessageAt: '2026-04-02T12:00:00Z',
      otherParticipants: [{ userId: 'user-3', displayName: 'Charlie' }],
      unreadCount: 0,
    },
  ];

  beforeEach(() => {
    messageSubject = new Subject<MessagePayload>();
    reconnectedSubject = new Subject<void>();

    const mockSignalR = {
      messageReceived: messageSubject.asObservable(),
      reconnected: reconnectedSubject.asObservable(),
      joinConversation: jest.fn().mockResolvedValue(undefined),
    };

    const mockAuthService = {
      currentUser$: new BehaviorSubject({ userId: 'user-1', displayName: 'Alice', email: 'alice@test.com', role: 'User' }),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SignalRService, useValue: mockSignalR },
        { provide: AuthService, useValue: mockAuthService },
      ],
    });

    service = TestBed.inject(ConversationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should load conversations from API', () => {
    let result: Conversation[] = [];
    service.conversations.subscribe(c => result = c);

    service.loadConversations();
    const req = httpMock.expectOne('/api/conversations');
    expect(req.request.method).toBe('GET');
    req.flush(mockConversations);

    expect(result.length).toBe(2);
    expect(result[0].id).toBe(1);
  });

  it('should set loading state during load', () => {
    const loadingStates: boolean[] = [];
    service.loading.subscribe(l => loadingStates.push(l));

    service.loadConversations();
    const req = httpMock.expectOne('/api/conversations');
    req.flush(mockConversations);

    expect(loadingStates).toContain(true);
    expect(loadingStates[loadingStates.length - 1]).toBe(false);
  });

  it('should select conversation', () => {
    let selected: Conversation | null = null;
    service.selectedConversation.subscribe(c => selected = c);

    service.selectConversation(mockConversations[0]);
    expect(selected).toEqual(mockConversations[0]);
  });

  it('should clear selection', () => {
    let selected: Conversation | null = null;
    service.selectedConversation.subscribe(c => selected = c);

    service.selectConversation(mockConversations[0]);
    service.clearSelection();
    expect(selected).toBeNull();
  });

  it('should return display name for private conversation', () => {
    const name = service.getDisplayName(mockConversations[0]);
    expect(name).toBe('Bob');
  });

  it('should return conversation name for group conversation', () => {
    const group: Conversation = {
      ...mockConversations[0],
      type: 'Group',
      name: 'Engineering',
    };
    const name = service.getDisplayName(group);
    expect(name).toBe('Engineering');
  });

  it('should move conversation to top on incoming message', () => {
    let result: Conversation[] = [];
    service.conversations.subscribe(c => result = c);

    service.loadConversations();
    httpMock.expectOne('/api/conversations').flush(mockConversations);

    // Message arrives for conversation 1 (currently second after sorting)
    messageSubject.next({
      id: 10,
      conversationId: 1,
      senderId: 'user-2',
      senderDisplayName: 'Bob',
      content: 'New message!',
      sentAt: '2026-04-02T14:00:00Z',
      messageType: 'Text',
    });

    expect(result[0].id).toBe(1);
    expect(result[0].lastMessagePreview).toBe('New message!');
  });

  it('should increment unread count for non-selected conversation', () => {
    let result: Conversation[] = [];
    service.conversations.subscribe(c => result = c);

    service.loadConversations();
    httpMock.expectOne('/api/conversations').flush(mockConversations);

    // Select conversation 2
    service.selectConversation(mockConversations[1]);

    // Message for conversation 1 (not selected)
    messageSubject.next({
      id: 10,
      conversationId: 1,
      senderId: 'user-2',
      senderDisplayName: 'Bob',
      content: 'New msg',
      sentAt: '2026-04-02T14:00:00Z',
      messageType: 'Text',
    });

    const conv1 = result.find(c => c.id === 1);
    expect(conv1?.unreadCount).toBe(3); // Was 2, now 3
  });

  describe('reconnection', () => {
    it('should reload conversations on reconnect', () => {
      let result: Conversation[] = [];
      service.conversations.subscribe(c => result = c);

      reconnectedSubject.next();
      const req = httpMock.expectOne('/api/conversations');
      expect(req.request.method).toBe('GET');
      req.flush(mockConversations);

      expect(result.length).toBe(2);
    });
  });

  describe('createConversation', () => {
    it('should create conversation and reload list', () => {
      let createdId: number | undefined;
      service.conversationCreated.subscribe(id => createdId = id);

      service.createConversation('user-2');

      // First: POST to create
      const createReq = httpMock.expectOne('/api/conversations');
      expect(createReq.request.method).toBe('POST');
      expect(createReq.request.body).toEqual({ otherUserId: 'user-2' });
      createReq.flush({ id: 10 } as CreateConversationResponse);

      // Second: GET to reload conversations
      const loadReq = httpMock.expectOne('/api/conversations');
      expect(loadReq.request.method).toBe('GET');
      loadReq.flush(mockConversations);

      expect(createdId).toBe(10);
    });

    it('should select the created conversation after reload', () => {
      let selected: Conversation | null = null;
      service.selectedConversation.subscribe(c => selected = c);

      const newConversation: Conversation = {
        id: 10,
        type: 'Private',
        name: null,
        lastMessagePreview: null,
        lastMessageAt: null,
        otherParticipants: [{ userId: 'user-2', displayName: 'Bob' }],
        unreadCount: 0,
      };

      service.createConversation('user-2');
      httpMock.expectOne({ method: 'POST', url: '/api/conversations' }).flush({ id: 10 });
      httpMock.expectOne({ method: 'GET', url: '/api/conversations' }).flush([newConversation, ...mockConversations]);

      expect(selected?.id).toBe(10);
    });

    it('should set error on create failure', () => {
      let error: string | null = null;
      service.error.subscribe(e => error = e);

      service.createConversation('user-2');
      httpMock.expectOne('/api/conversations').flush('Error', { status: 500, statusText: 'Error' });

      expect(error).toBe('Failed to create conversation');
    });

    it('should emit conversationCreated even if reload fails', () => {
      let createdId: number | undefined;
      let error: string | null = null;
      service.conversationCreated.subscribe(id => createdId = id);
      service.error.subscribe(e => error = e);

      service.createConversation('user-2');
      httpMock.expectOne({ method: 'POST', url: '/api/conversations' }).flush({ id: 10 });
      httpMock.expectOne({ method: 'GET', url: '/api/conversations' }).flush('Error', { status: 500, statusText: 'Error' });

      expect(createdId).toBe(10);
      expect(error).toBe('Conversation created but failed to refresh list');
    });
  });
});
