import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ConversationListComponent } from '../conversation-list/conversation-list.component';
import { ChatWindowComponent } from '../chat-window/chat-window.component';
import { OnlineUsersComponent } from '../online-users/online-users.component';
import { MessageInputComponent } from '../message-input/message-input.component';
import { ConversationService } from '../../services/conversation.service';
import { Conversation } from '../../models/conversation.model';

export type ActivePanel = 'list' | 'chat' | 'members';
export type LayoutMode = 'desktop' | 'tablet' | 'mobile';

@Component({
  selector: 'app-chat-layout',
  standalone: true,
  imports: [AsyncPipe, ConversationListComponent, ChatWindowComponent, OnlineUsersComponent, MessageInputComponent],
  templateUrl: './chat-layout.component.html',
  styleUrl: './chat-layout.component.scss',
})
export class ChatLayoutComponent implements OnInit, OnDestroy {
  private readonly conversationService = inject(ConversationService);

  readonly selectedConversation$ = this.conversationService.selectedConversation;

  layoutMode: LayoutMode = 'desktop';
  activePanel: ActivePanel = 'list';
  showMembersPanel = false;
  sidebarCollapsed = false;

  private mobileQuery!: MediaQueryList;
  private tabletQuery!: MediaQueryList;
  private readonly mobileListener = (): void => this.onBreakpointChange();
  private readonly tabletListener = (): void => this.onBreakpointChange();

  ngOnInit(): void {
    this.mobileQuery = window.matchMedia('(max-width: 767px)');
    this.tabletQuery = window.matchMedia('(min-width: 768px) and (max-width: 1023px)');

    this.mobileQuery.addEventListener('change', this.mobileListener);
    this.tabletQuery.addEventListener('change', this.tabletListener);

    this.onBreakpointChange();
  }

  ngOnDestroy(): void {
    this.mobileQuery.removeEventListener('change', this.mobileListener);
    this.tabletQuery.removeEventListener('change', this.tabletListener);
  }

  get isMobile(): boolean {
    return this.layoutMode === 'mobile';
  }

  get isTablet(): boolean {
    return this.layoutMode === 'tablet';
  }

  get isDesktop(): boolean {
    return this.layoutMode === 'desktop';
  }

  get showSidebar(): boolean {
    if (this.isMobile) {
      return this.activePanel === 'list';
    }
    if (this.isTablet) {
      return !this.sidebarCollapsed;
    }
    return true;
  }

  get showChatArea(): boolean {
    if (this.isMobile) {
      return this.activePanel === 'chat';
    }
    return true;
  }

  get showMembers(): boolean {
    if (this.isMobile) {
      return this.activePanel === 'members';
    }
    if (this.isTablet) {
      return false;
    }
    return this.showMembersPanel;
  }

  onConversationSelected(conversation: Conversation): void {
    void conversation; // Used by template binding; panel switch handled here
    if (this.isMobile) {
      this.activePanel = 'chat';
    }
  }

  getConversationTitle(conversation: Conversation | null): string {
    if (!conversation) {
      return 'Select a conversation';
    }
    return this.conversationService.getDisplayName(conversation);
  }

  navigateBack(): void {
    this.activePanel = 'list';
  }

  toggleMembers(): void {
    if (this.isMobile) {
      this.activePanel = this.activePanel === 'members' ? 'chat' : 'members';
    } else {
      this.showMembersPanel = !this.showMembersPanel;
    }
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  private onBreakpointChange(): void {
    if (this.mobileQuery.matches) {
      this.layoutMode = 'mobile';
      this.showMembersPanel = false;
      this.sidebarCollapsed = false;
    } else if (this.tabletQuery.matches) {
      this.layoutMode = 'tablet';
      this.showMembersPanel = false;
      this.activePanel = this.activePanel === 'members' ? 'chat' : this.activePanel;
    } else {
      this.layoutMode = 'desktop';
      this.sidebarCollapsed = false;
      this.activePanel = this.activePanel === 'members' ? 'chat' : this.activePanel;
    }
  }
}
