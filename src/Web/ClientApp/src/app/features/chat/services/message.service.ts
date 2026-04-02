import { Injectable, inject, DestroyRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, Subscription } from 'rxjs';
import { Message, MessageHistoryResponse } from '../models/message.model';
import { SignalRService } from '../../../core/signalr/signalr.service';
import { MessagePayload } from '../../../core/signalr/signalr.events';
import { AuthService } from '../../../core/services/auth.service';

@Injectable({ providedIn: 'root' })
export class MessageService {
  private readonly http = inject(HttpClient);
  private readonly signalRService = inject(SignalRService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _messages$ = new BehaviorSubject<Message[]>([]);
  private readonly _loading$ = new BehaviorSubject<boolean>(false);
  private readonly _loadingHistory$ = new BehaviorSubject<boolean>(false);
  private readonly _hasMore$ = new BehaviorSubject<boolean>(false);
  private readonly _error$ = new BehaviorSubject<string | null>(null);
  private readonly _sendError$ = new BehaviorSubject<string | null>(null);
  private readonly _sendConfirmed$ = new Subject<number>();

  private currentConversationId: number | null = null;
  private currentUserId: string | null = null;
  private readonly scrollPositions = new Map<number, number>();
  private readonly firstMessageSent = new Map<number, boolean>();
  private readonly pendingMessageIds = new Set<number>();
  private tempIdCounter = 0;
  private loadSubscription: Subscription | null = null;

  readonly messages$: Observable<Message[]> = this._messages$.asObservable();
  readonly loading$: Observable<boolean> = this._loading$.asObservable();
  readonly loadingHistory$: Observable<boolean> = this._loadingHistory$.asObservable();
  readonly hasMore$: Observable<boolean> = this._hasMore$.asObservable();
  readonly error$: Observable<string | null> = this._error$.asObservable();
  readonly sendError$: Observable<string | null> = this._sendError$.asObservable();
  readonly sendConfirmed$: Observable<number> = this._sendConfirmed$.asObservable();

  constructor() {
    const msgSub = this.signalRService.messageReceived.subscribe(
      (message: MessagePayload) => this.handleIncomingMessage(message)
    );
    const userSub = this.authService.currentUser$.subscribe(
      (user) => this.currentUserId = user?.userId ?? null
    );
    this.destroyRef.onDestroy(() => {
      msgSub.unsubscribe();
      userSub.unsubscribe();
    });
  }

  loadMessages(conversationId: number, before?: number): void {
    if (before === undefined) {
      // Initial load — cancel any in-flight request
      this.loadSubscription?.unsubscribe();
      this.currentConversationId = conversationId;
      this._loading$.next(true);
      this._error$.next(null);
    } else {
      this._loadingHistory$.next(true);
    }

    let url = `/api/conversations/${conversationId}/messages`;
    const params: string[] = [];
    if (before !== undefined) {
      params.push(`before=${before}`);
    }
    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    this.loadSubscription = this.http.get<MessageHistoryResponse>(url).subscribe({
      next: (response: MessageHistoryResponse) => {
        // Guard against stale responses after conversation switch
        if (conversationId !== this.currentConversationId) {
          return;
        }

        const newMessages = [...response.messages].reverse();

        if (before !== undefined) {
          // Prepend older messages
          const current = this._messages$.value;
          this._messages$.next([...newMessages, ...current]);
          this._loadingHistory$.next(false);
        } else {
          // Initial load — replace
          this._messages$.next(newMessages);
        }

        this._hasMore$.next(response.hasMore);
        this._loading$.next(false);
      },
      error: () => {
        this._error$.next('Failed to load messages');
        this._loading$.next(false);
        this._loadingHistory$.next(false);
      },
    });
  }

  clearMessages(): void {
    this._messages$.next([]);
    this._hasMore$.next(false);
    this._error$.next(null);
    this._sendError$.next(null);
    this.currentConversationId = null;
    this.pendingMessageIds.clear();
    this.firstMessageSent.clear();
  }

  saveScrollPosition(conversationId: number, scrollTop: number): void {
    this.scrollPositions.set(conversationId, scrollTop);
  }

  getScrollPosition(conversationId: number): number | undefined {
    return this.scrollPositions.get(conversationId);
  }

  getOldestMessageId(): number | undefined {
    const messages = this._messages$.value;
    if (messages.length === 0) {
      return undefined;
    }
    return messages[0].id;
  }

  isOwnMessage(senderId: string): boolean {
    return this.currentUserId !== null && senderId === this.currentUserId;
  }

  isFirstMessageInConversation(conversationId: number): boolean {
    return !this.firstMessageSent.get(conversationId);
  }

  sendMessage(conversationId: number, content: string): { tempId: number } {
    const tempId = -(Date.now() * 1000 + ++this.tempIdCounter);
    const capturedConversationId = conversationId;
    this._sendError$.next(null);

    const optimisticMessage: Message = {
      id: tempId,
      conversationId,
      senderId: this.currentUserId ?? '',
      senderDisplayName: '',
      content,
      sentAt: new Date().toISOString(),
      messageType: 'Text',
      checkmarkState: 'sending',
    };

    const current = this._messages$.value;
    this._messages$.next([...current, optimisticMessage]);

    this.signalRService.sendMessage(conversationId, content)
      .then((persistedId: number) => {
        if (this.currentConversationId !== capturedConversationId) {
          return;
        }
        this.pendingMessageIds.add(persistedId);
        this.firstMessageSent.set(conversationId, true);
        const messages = this._messages$.value;
        this._messages$.next(
          messages.map(m => m.id === tempId
            ? { ...m, id: persistedId, checkmarkState: 'sent' as const }
            : m
          )
        );
        this._sendConfirmed$.next(capturedConversationId);
      })
      .catch(() => {
        if (this.currentConversationId !== capturedConversationId) {
          return;
        }
        // Remove optimistic message on failure
        const messages = this._messages$.value;
        this._messages$.next(messages.filter(m => m.id !== tempId));
        this._sendError$.next('Couldn\'t send \u2014 tap to retry');
      });

    return { tempId };
  }

  clearSendError(): void {
    this._sendError$.next(null);
  }

  private handleIncomingMessage(payload: MessagePayload): void {
    if (this.currentConversationId === null || payload.conversationId !== this.currentConversationId) {
      return;
    }

    // Skip duplicate if this message was already confirmed via optimistic send
    if (this.pendingMessageIds.has(payload.id)) {
      this.pendingMessageIds.delete(payload.id);
      return;
    }

    // Handle race: SignalR echo arrives before .then() adds to pendingMessageIds
    const currentMessages = this._messages$.value;
    const optimisticMatch = currentMessages.find(m =>
      m.id < 0 && m.senderId === payload.senderId && m.content === payload.content
    );
    if (optimisticMatch) {
      this.pendingMessageIds.add(payload.id);
      this._messages$.next(
        currentMessages.map(m => m.id === optimisticMatch.id
          ? { ...m, id: payload.id, checkmarkState: 'sent' as const }
          : m
        )
      );
      this.firstMessageSent.set(payload.conversationId, true);
      this._sendConfirmed$.next(payload.conversationId);
      return;
    }

    const message: Message = {
      id: payload.id,
      conversationId: payload.conversationId,
      senderId: payload.senderId,
      senderDisplayName: payload.senderDisplayName,
      content: payload.content,
      sentAt: payload.sentAt,
      messageType: payload.messageType,
    };

    const current = this._messages$.value;
    this._messages$.next([...current, message]);
  }
}
