import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { MessageService } from './message.service';
import { MessageHistoryResponse } from '../models/message.model';
import { SignalRService } from '../../../core/signalr/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { Subject, BehaviorSubject } from 'rxjs';
import { MessagePayload } from '../../../core/signalr/signalr.events';

describe('MessageService', () => {
  let service: MessageService;
  let httpTesting: HttpTestingController;
  let messageReceivedSubject: Subject<MessagePayload>;

  const mockResponse: MessageHistoryResponse = {
    messages: [
      { id: 3, conversationId: 1, senderId: 'user-2', senderDisplayName: 'Other', content: 'Hi', sentAt: '2026-04-01T10:00:00Z', messageType: 'Text' },
      { id: 2, conversationId: 1, senderId: 'user-1', senderDisplayName: 'Me', content: 'Hello', sentAt: '2026-04-01T09:00:00Z', messageType: 'Text' },
      { id: 1, conversationId: 1, senderId: 'user-2', senderDisplayName: 'Other', content: 'Hey', sentAt: '2026-04-01T08:00:00Z', messageType: 'Text' },
    ],
    hasMore: true,
    nextCursor: 1,
  };

  beforeEach(() => {
    messageReceivedSubject = new Subject<MessagePayload>();

    const mockSignalR = {
      messageReceived: messageReceivedSubject.asObservable(),
    };

    const mockAuth = {
      currentUser$: new BehaviorSubject({ userId: 'user-1', displayName: 'Test', email: 'test@test.com', role: 'Member' }),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SignalRService, useValue: mockSignalR },
        { provide: AuthService, useValue: mockAuth },
      ],
    });

    service = TestBed.inject(MessageService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('loadMessages', () => {
    it('should load and reverse messages for initial load', () => {
      let messages: unknown[] = [];
      service.messages$.subscribe(m => messages = m);

      service.loadMessages(1);

      const req = httpTesting.expectOne('/api/conversations/1/messages');
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);

      expect(messages.length).toBe(3);
      expect((messages[0] as { id: number }).id).toBe(1);
      expect((messages[1] as { id: number }).id).toBe(2);
      expect((messages[2] as { id: number }).id).toBe(3);
    });

    it('should set loading state during initial load', () => {
      const loadingStates: boolean[] = [];
      service.loading$.subscribe(l => loadingStates.push(l));

      service.loadMessages(1);
      expect(loadingStates).toContain(true);

      const req = httpTesting.expectOne('/api/conversations/1/messages');
      req.flush(mockResponse);

      expect(loadingStates[loadingStates.length - 1]).toBe(false);
    });

    it('should set hasMore from response', () => {
      let hasMore = false;
      service.hasMore$.subscribe(h => hasMore = h);

      service.loadMessages(1);
      const req = httpTesting.expectOne('/api/conversations/1/messages');
      req.flush(mockResponse);

      expect(hasMore).toBe(true);
    });

    it('should prepend older messages on pagination load', () => {
      service.loadMessages(1);
      const req1 = httpTesting.expectOne('/api/conversations/1/messages');
      req1.flush({
        messages: [
          { id: 5, conversationId: 1, senderId: 'user-1', senderDisplayName: 'Me', content: 'New', sentAt: '2026-04-01T12:00:00Z', messageType: 'Text' },
        ],
        hasMore: true,
        nextCursor: 5,
      });

      service.loadMessages(1, 5);
      const req2 = httpTesting.expectOne('/api/conversations/1/messages?before=5');
      req2.flush({
        messages: [
          { id: 2, conversationId: 1, senderId: 'user-2', senderDisplayName: 'Other', content: 'Old2', sentAt: '2026-04-01T09:00:00Z', messageType: 'Text' },
          { id: 1, conversationId: 1, senderId: 'user-2', senderDisplayName: 'Other', content: 'Old1', sentAt: '2026-04-01T08:00:00Z', messageType: 'Text' },
        ],
        hasMore: false,
        nextCursor: null,
      });

      let messages: unknown[] = [];
      service.messages$.subscribe(m => messages = m);

      expect(messages.length).toBe(3);
      expect((messages[0] as { id: number }).id).toBe(1);
      expect((messages[1] as { id: number }).id).toBe(2);
      expect((messages[2] as { id: number }).id).toBe(5);
    });

    it('should set error on failure', () => {
      let error: string | null = null;
      service.error$.subscribe(e => error = e);

      service.loadMessages(1);
      const req = httpTesting.expectOne('/api/conversations/1/messages');
      req.error(new ProgressEvent('error'));

      expect(error).toBe('Failed to load messages');
    });

    it('should cancel previous request when switching conversations', () => {
      let messages: unknown[] = [];
      service.messages$.subscribe(m => messages = m);

      // Start loading conversation 1
      service.loadMessages(1);
      const req1 = httpTesting.expectOne('/api/conversations/1/messages');

      // Switch to conversation 2 — cancels conv 1 request
      service.loadMessages(2);

      // Conv 1 request was cancelled
      expect(req1.cancelled).toBe(true);

      // Flush conversation 2 response
      const req2 = httpTesting.expectOne('/api/conversations/2/messages');
      req2.flush({
        messages: [{ id: 10, conversationId: 2, senderId: 'user-2', senderDisplayName: 'Other', content: 'Conv2', sentAt: '2026-04-01T12:00:00Z', messageType: 'Text' }],
        hasMore: false,
        nextCursor: null,
      });

      expect(messages.length).toBe(1);
      expect((messages[0] as { id: number }).id).toBe(10);
    });

    it('should set loadingHistory$ during pagination load', () => {
      const historyStates: boolean[] = [];
      service.loadingHistory$.subscribe(h => historyStates.push(h));

      service.loadMessages(1);
      const req1 = httpTesting.expectOne('/api/conversations/1/messages');
      req1.flush({
        messages: [{ id: 5, conversationId: 1, senderId: 'user-1', senderDisplayName: 'Me', content: 'Msg', sentAt: '2026-04-01T12:00:00Z', messageType: 'Text' }],
        hasMore: true,
        nextCursor: 5,
      });

      service.loadMessages(1, 5);
      expect(historyStates).toContain(true);

      const req2 = httpTesting.expectOne('/api/conversations/1/messages?before=5');
      req2.flush({ messages: [], hasMore: false, nextCursor: null });

      expect(historyStates[historyStates.length - 1]).toBe(false);
    });

    it('should reset loadingHistory$ on pagination error', () => {
      let loadingHistory = false;
      service.loadingHistory$.subscribe(h => loadingHistory = h);

      service.loadMessages(1);
      const req1 = httpTesting.expectOne('/api/conversations/1/messages');
      req1.flush({
        messages: [{ id: 5, conversationId: 1, senderId: 'user-1', senderDisplayName: 'Me', content: 'Msg', sentAt: '2026-04-01T12:00:00Z', messageType: 'Text' }],
        hasMore: true,
        nextCursor: 5,
      });

      service.loadMessages(1, 5);
      const req2 = httpTesting.expectOne('/api/conversations/1/messages?before=5');
      req2.error(new ProgressEvent('error'));

      expect(loadingHistory).toBe(false);
    });
  });

  describe('clearMessages', () => {
    it('should clear all state', () => {
      service.loadMessages(1);
      const req = httpTesting.expectOne('/api/conversations/1/messages');
      req.flush(mockResponse);

      service.clearMessages();

      let messages: unknown[] = [];
      let hasMore = true;
      service.messages$.subscribe(m => messages = m);
      service.hasMore$.subscribe(h => hasMore = h);

      expect(messages.length).toBe(0);
      expect(hasMore).toBe(false);
    });
  });

  describe('handleIncomingMessage', () => {
    it('should append new message for active conversation', () => {
      service.loadMessages(1);
      const req = httpTesting.expectOne('/api/conversations/1/messages');
      req.flush({ messages: [], hasMore: false, nextCursor: null });

      const payload: MessagePayload = {
        id: 10,
        conversationId: 1,
        senderId: 'user-2',
        senderDisplayName: 'Other',
        content: 'New message',
        sentAt: '2026-04-01T14:00:00Z',
        messageType: 'Text',
      };
      messageReceivedSubject.next(payload);

      let messages: unknown[] = [];
      service.messages$.subscribe(m => messages = m);

      expect(messages.length).toBe(1);
      expect((messages[0] as { id: number }).id).toBe(10);
    });

    it('should ignore messages for different conversation', () => {
      service.loadMessages(1);
      const req = httpTesting.expectOne('/api/conversations/1/messages');
      req.flush({ messages: [], hasMore: false, nextCursor: null });

      const payload: MessagePayload = {
        id: 10,
        conversationId: 99,
        senderId: 'user-2',
        senderDisplayName: 'Other',
        content: 'Wrong conv',
        sentAt: '2026-04-01T14:00:00Z',
        messageType: 'Text',
      };
      messageReceivedSubject.next(payload);

      let messages: unknown[] = [];
      service.messages$.subscribe(m => messages = m);

      expect(messages.length).toBe(0);
    });

    it('should ignore messages when no conversation is active', () => {
      const payload: MessagePayload = {
        id: 10,
        conversationId: 1,
        senderId: 'user-2',
        senderDisplayName: 'Other',
        content: 'No active',
        sentAt: '2026-04-01T14:00:00Z',
        messageType: 'Text',
      };
      messageReceivedSubject.next(payload);

      let messages: unknown[] = [];
      service.messages$.subscribe(m => messages = m);

      expect(messages.length).toBe(0);
    });
  });

  describe('scroll position cache', () => {
    it('should save and retrieve scroll positions', () => {
      service.saveScrollPosition(1, 500);
      service.saveScrollPosition(2, 300);

      expect(service.getScrollPosition(1)).toBe(500);
      expect(service.getScrollPosition(2)).toBe(300);
      expect(service.getScrollPosition(3)).toBeUndefined();
    });
  });

  describe('getOldestMessageId', () => {
    it('should return undefined when no messages', () => {
      expect(service.getOldestMessageId()).toBeUndefined();
    });

    it('should return the first message id', () => {
      service.loadMessages(1);
      const req = httpTesting.expectOne('/api/conversations/1/messages');
      req.flush(mockResponse);

      expect(service.getOldestMessageId()).toBe(1);
    });
  });

  describe('isOwnMessage', () => {
    it('should return true for own messages', () => {
      expect(service.isOwnMessage('user-1')).toBe(true);
    });

    it('should return false for other messages', () => {
      expect(service.isOwnMessage('user-2')).toBe(false);
    });
  });
});
