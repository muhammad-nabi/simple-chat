import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { pairwise, startWith } from 'rxjs/operators';
import { MessageBubbleComponent } from '../message-bubble/message-bubble.component';
import { GroupMembersComponent } from '../group-members/group-members.component';
import { InviteToGroupComponent } from '../invite-to-group/invite-to-group.component';
import { MessageService } from '../../services/message.service';
import { ConversationService } from '../../services/conversation.service';
import { AuthService } from '../../../../core/services/auth.service';
import { SignalRService } from '../../../../core/signalr/signalr.service';
import { Message } from '../../models/message.model';
import { Conversation } from '../../models/conversation.model';

@Component({
  selector: 'app-chat-window',
  standalone: true,
  imports: [AsyncPipe, MessageBubbleComponent, GroupMembersComponent, InviteToGroupComponent],
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.scss',
})
export class ChatWindowComponent implements OnInit, OnDestroy, AfterViewChecked {
  private readonly messageService = inject(MessageService);
  private readonly conversationService = inject(ConversationService);
  private readonly authService = inject(AuthService);
  private readonly signalRService = inject(SignalRService);

  @ViewChild('scrollContainer') scrollContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('membersPanel') membersPanel!: GroupMembersComponent;

  messages: Message[] = [];
  loading = false;
  loadingHistory = false;
  hasMore = false;
  hasNewMessages = false;
  selectedConversation: Conversation | null = null;
  showMembersPanel = false;
  showInviteDialog = false;
  memberIds: string[] = [];

  private currentUserId: string | null = null;
  private shouldScrollToBottom = false;
  private shouldRestoreScroll = false;
  private isLoadingHistory = false;
  private previousScrollHeight = 0;
  private previousScrollTop = 0;
  private subscriptions: Subscription[] = [];

  ngOnInit(): void {
    const userSub = this.authService.currentUser$.subscribe(
      (user) => this.currentUserId = user?.userId ?? null
    );

    const msgSub = this.messageService.messages$.subscribe((messages: Message[]) => {
      const isNewMessage = messages.length > this.messages.length && !this.isLoadingHistory;

      if (isNewMessage && this.isNearBottom()) {
        this.shouldScrollToBottom = true;
      } else if (isNewMessage && !this.isNearBottom()) {
        this.hasNewMessages = true;
      }

      this.messages = messages;
    });

    const loadingSub = this.messageService.loading$.subscribe(
      (loading: boolean) => this.loading = loading
    );

    const loadingHistorySub = this.messageService.loadingHistory$.subscribe(
      (loadingHistory: boolean) => {
        this.loadingHistory = loadingHistory;
        // Reset isLoadingHistory flag when service reports history load complete
        if (!loadingHistory && this.isLoadingHistory) {
          // History load finished (success or error) — clear component flag
          this.isLoadingHistory = false;
        }
      }
    );

    const hasMoreSub = this.messageService.hasMore$.subscribe(
      (hasMore: boolean) => this.hasMore = hasMore
    );

    const convSub = this.conversationService.selectedConversation.pipe(
      startWith(null as Conversation | null),
      pairwise(),
    ).subscribe(([prev, current]) => {
      // Save scroll position of previous conversation
      if (prev && this.scrollContainer) {
        this.messageService.saveScrollPosition(prev.id, this.scrollContainer.nativeElement.scrollTop);
      }

      this.selectedConversation = current;
      this.hasNewMessages = false;

      if (current) {
        this.messageService.loadMessages(current.id);
        // Restore saved scroll position or scroll to bottom for first visit
        const savedPosition = this.messageService.getScrollPosition(current.id);
        if (savedPosition !== undefined) {
          this.shouldRestoreScroll = true;
        } else {
          this.shouldScrollToBottom = true;
        }
      } else {
        this.messageService.clearMessages();
      }
    });

    const reconnectSub = this.signalRService.reconnected.subscribe(() => {
      if (this.selectedConversation) {
        this.messageService.loadMessages(this.selectedConversation.id);
        this.shouldScrollToBottom = true;
      }
    });

    this.subscriptions.push(userSub, msgSub, loadingSub, loadingHistorySub, hasMoreSub, convSub, reconnectSub);
  }

  ngAfterViewChecked(): void {
    if (this.shouldRestoreScroll && this.scrollContainer && this.messages.length > 0) {
      const savedPosition = this.selectedConversation
        ? this.messageService.getScrollPosition(this.selectedConversation.id)
        : undefined;
      if (savedPosition !== undefined) {
        this.scrollContainer.nativeElement.scrollTop = savedPosition;
      }
      this.shouldRestoreScroll = false;
    }

    if (this.shouldScrollToBottom && this.scrollContainer) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }

    if (this.isLoadingHistory && this.scrollContainer && !this.loadingHistory) {
      const el = this.scrollContainer.nativeElement;
      const newScrollHeight = el.scrollHeight;
      el.scrollTop = newScrollHeight - this.previousScrollHeight + this.previousScrollTop;
      this.isLoadingHistory = false;
    }
  }

  ngOnDestroy(): void {
    if (this.selectedConversation && this.scrollContainer) {
      this.messageService.saveScrollPosition(
        this.selectedConversation.id,
        this.scrollContainer.nativeElement.scrollTop,
      );
    }
    this.subscriptions.forEach(s => s.unsubscribe());
  }

  onScroll(): void {
    const el = this.scrollContainer?.nativeElement;
    if (!el) {
      return;
    }

    // Infinite scroll up — require scrollTop < 1 (not strict 0) to avoid
    // auto-triggering on short lists where scrollTop starts at 0
    if (el.scrollTop < 1 && this.hasMore && !this.loading && !this.isLoadingHistory && el.scrollHeight > el.clientHeight) {
      this.loadOlderMessages();
    }

    // Reset new messages indicator when scrolling to bottom
    if (this.isNearBottom()) {
      this.hasNewMessages = false;
    }
  }

  scrollToBottom(): void {
    if (!this.scrollContainer) {
      return;
    }
    const el = this.scrollContainer.nativeElement;
    el.scrollTop = el.scrollHeight;
    this.hasNewMessages = false;
  }

  isOwnMessage(senderId: string): boolean {
    return this.messageService.isOwnMessage(senderId);
  }

  isSystemMessage(message: Message): boolean {
    return message.messageType === 'System';
  }

  isGroupConversation(): boolean {
    return this.selectedConversation?.type === 'Group';
  }

  shouldShowSender(index: number): boolean {
    if (!this.isGroupConversation()) {
      return false;
    }
    const message = this.messages[index];
    if (message.messageType === 'System') {
      return false;
    }
    if (index === 0) {
      return true;
    }
    const prev = this.messages[index - 1];
    if (prev.messageType === 'System') {
      return true;
    }
    return message.senderId !== prev.senderId;
  }

  isConsecutiveMessage(index: number): boolean {
    if (index === 0) {
      return false;
    }
    const message = this.messages[index];
    const prev = this.messages[index - 1];
    if (message.messageType === 'System' || prev.messageType === 'System') {
      return false;
    }
    return message.senderId === prev.senderId;
  }

  isFirstSentMessage(): boolean {
    return this.selectedConversation !== null
      && this.messageService.isFirstMessageInConversation(this.selectedConversation.id);
  }

  trackByMessageId(_index: number, message: Message): number {
    return message.id;
  }

  toggleMembersPanel(): void {
    this.showMembersPanel = !this.showMembersPanel;
    if (!this.showMembersPanel) {
      this.showInviteDialog = false;
    }
  }

  onInviteRequested(): void {
    if (this.membersPanel) {
      this.memberIds = this.membersPanel.getMemberIds();
    }
    this.showInviteDialog = true;
  }

  onLeftGroup(): void {
    this.showMembersPanel = false;
    this.showInviteDialog = false;
  }

  onMembersPanelClosed(): void {
    this.showMembersPanel = false;
    this.showInviteDialog = false;
  }

  onInvited(): void {
    this.showInviteDialog = false;
    if (this.membersPanel) {
      this.membersPanel.loadMembers();
    }
  }

  onInviteClosed(): void {
    this.showInviteDialog = false;
  }

  private isNearBottom(): boolean {
    if (!this.scrollContainer) {
      return true;
    }
    const el = this.scrollContainer.nativeElement;
    return el.scrollHeight - el.scrollTop - el.clientHeight < 50;
  }

  private loadOlderMessages(): void {
    const oldestId = this.messageService.getOldestMessageId();
    if (oldestId === undefined || !this.selectedConversation) {
      return;
    }

    this.isLoadingHistory = true;
    const el = this.scrollContainer.nativeElement;
    this.previousScrollHeight = el.scrollHeight;
    this.previousScrollTop = el.scrollTop;
    this.messageService.loadMessages(this.selectedConversation.id, oldestId);
  }
}
