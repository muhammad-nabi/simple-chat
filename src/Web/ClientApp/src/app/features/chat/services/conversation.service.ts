import { Injectable, inject, DestroyRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { Conversation } from '../models/conversation.model';
import { SignalRService } from '../../../core/signalr/signalr.service';
import { MessagePayload } from '../../../core/signalr/signalr.events';
import { AuthService } from '../../../core/services/auth.service';

export interface CreateConversationResponse {
  id: number;
}

export interface BrowseGroupDto {
  id: number;
  name: string;
  participantCount: number;
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class ConversationService {
  private readonly http = inject(HttpClient);
  private readonly signalRService = inject(SignalRService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _conversations$ = new BehaviorSubject<Conversation[]>([]);
  private readonly _selectedConversation$ = new BehaviorSubject<Conversation | null>(null);
  private readonly _loading$ = new BehaviorSubject<boolean>(false);
  private readonly _error$ = new BehaviorSubject<string | null>(null);
  private currentUserId: string | null = null;

  private readonly _conversationCreated$ = new Subject<number>();

  readonly conversations: Observable<Conversation[]> = this._conversations$.asObservable();
  readonly selectedConversation: Observable<Conversation | null> = this._selectedConversation$.asObservable();
  readonly loading: Observable<boolean> = this._loading$.asObservable();
  readonly error: Observable<string | null> = this._error$.asObservable();
  readonly conversationCreated: Observable<number> = this._conversationCreated$.asObservable();

  constructor() {
    const msgSub = this.signalRService.messageReceived.subscribe(
      (message: MessagePayload) => this.handleIncomingMessage(message)
    );
    const userSub = this.authService.currentUser$.subscribe(
      (user) => this.currentUserId = user?.userId ?? null
    );
    const reconnectSub = this.signalRService.reconnected.subscribe(() => {
      this.loadConversations();
    });
    this.destroyRef.onDestroy(() => {
      msgSub.unsubscribe();
      userSub.unsubscribe();
      reconnectSub.unsubscribe();
    });
  }

  loadConversations(): void {
    this._loading$.next(true);
    this._error$.next(null);
    this.http.get<Conversation[]>('/api/conversations').subscribe({
      next: (conversations: Conversation[]) => {
        this._conversations$.next(conversations);
        this._loading$.next(false);
      },
      error: () => {
        this._error$.next('Failed to load conversations');
        this._loading$.next(false);
      },
    });
  }

  selectConversation(conversation: Conversation): void {
    this._selectedConversation$.next(conversation);
  }

  clearSelection(): void {
    this._selectedConversation$.next(null);
  }

  getDisplayName(conversation: Conversation): string {
    if (conversation.type === 'Private' && conversation.otherParticipants.length > 0) {
      return conversation.otherParticipants[0].displayName || 'Unknown User';
    }
    return conversation.name ?? 'Conversation';
  }

  createConversation(otherUserId: string): void {
    this._error$.next(null);
    this.http.post<CreateConversationResponse>('/api/conversations', { otherUserId }).subscribe({
      next: (response: CreateConversationResponse) => {
        this.handleConversationCreated(response.id);
      },
      error: () => {
        this._error$.next('Failed to create conversation');
      },
    });
  }

  createGroupConversation(participantIds: string[], groupName: string): void {
    this._error$.next(null);
    this.http.post<CreateConversationResponse>('/api/conversations', { participantIds, groupName }).subscribe({
      next: (response: CreateConversationResponse) => {
        this.handleConversationCreated(response.id);
      },
      error: () => {
        this._error$.next('Failed to create group conversation');
      },
    });
  }

  loadBrowseGroups(): Observable<BrowseGroupDto[]> {
    return this.http.get<BrowseGroupDto[]>('/api/conversations/browse');
  }

  joinGroup(conversationId: number): void {
    this._error$.next(null);
    this.http.post(`/api/conversations/${conversationId}/join`, null).subscribe({
      next: () => {
        // Join SignalR group for real-time messages immediately
        this.signalRService.joinConversation(conversationId).catch(() => {
          // Non-critical — messages may not arrive in real-time until reconnect
        });
        // Reload conversations, then select the joined group
        this.http.get<Conversation[]>('/api/conversations').subscribe({
          next: (conversations: Conversation[]) => {
            this._conversations$.next(conversations);
            const joined = conversations.find((c: Conversation) => c.id === conversationId);
            if (joined) {
              this._selectedConversation$.next(joined);
            }
            this._conversationCreated$.next(conversationId);
          },
          error: () => {
            this._error$.next('Joined group but failed to refresh list');
            this._conversationCreated$.next(conversationId);
          },
        });
      },
      error: () => {
        this._error$.next('Failed to join group');
      },
    });
  }

  private handleConversationCreated(conversationId: number): void {
    // Join SignalR group for the new conversation
    this.signalRService.joinConversation(conversationId).catch(() => {
      // Non-critical — messages may not arrive in real-time until reconnect
    });
    // Reload conversations, then select the new one
    this.http.get<Conversation[]>('/api/conversations').subscribe({
      next: (conversations: Conversation[]) => {
        this._conversations$.next(conversations);
        const created = conversations.find((c: Conversation) => c.id === conversationId);
        if (created) {
          this._selectedConversation$.next(created);
        }
        this._conversationCreated$.next(conversationId);
      },
      error: () => {
        // Conversations failed to reload — still emit so dialog closes, but warn
        this._error$.next('Conversation created but failed to refresh list');
        this._conversationCreated$.next(conversationId);
      },
    });
  }

  private handleIncomingMessage(message: MessagePayload): void {
    const current = this._conversations$.value;
    const index = current.findIndex(c => c.id === message.conversationId);

    if (index === -1) {
      // Conversation not in list — reload to pick up new conversations
      this.loadConversations();
      return;
    }

    const updated = [...current];
    const conversation = { ...updated[index] };
    conversation.lastMessagePreview = message.content;
    conversation.lastMessageAt = message.sentAt;

    // Increment unread if not the currently selected conversation and not own message
    const selected = this._selectedConversation$.value;
    if (message.senderId !== this.currentUserId && (!selected || selected.id !== message.conversationId)) {
      conversation.unreadCount = conversation.unreadCount + 1;
    }

    // Remove from current position and move to top
    updated.splice(index, 1);
    updated.unshift(conversation);

    this._conversations$.next(updated);
  }
}
