import { Component, inject, OnInit, OnDestroy, output } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { ConversationService } from '../../services/conversation.service';
import { Conversation } from '../../models/conversation.model';
import { TeamMember } from '../../models/user.model';
import { AvatarComponent } from '../../../../shared/components/avatar/avatar.component';
import { UnreadBadgeComponent } from '../../../../shared/components/unread-badge/unread-badge.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { NewChatDialogComponent } from '../new-chat-dialog/new-chat-dialog.component';

@Component({
  selector: 'app-conversation-list',
  standalone: true,
  imports: [AsyncPipe, AvatarComponent, UnreadBadgeComponent, RelativeTimePipe, NewChatDialogComponent],
  templateUrl: './conversation-list.component.html',
  styleUrl: './conversation-list.component.scss',
})
export class ConversationListComponent implements OnInit, OnDestroy {
  private readonly conversationService = inject(ConversationService);

  readonly conversations$ = this.conversationService.conversations;
  readonly selectedConversation$ = this.conversationService.selectedConversation;
  readonly loading$ = this.conversationService.loading;

  readonly conversationSelected = output<Conversation>();

  showNewChatDialog = false;

  private subscriptions: Subscription[] = [];

  ngOnInit(): void {
    this.conversationService.loadConversations();

    const createdSub = this.conversationService.conversationCreated.subscribe(() => {
      this.showNewChatDialog = false;
    });
    const selectedSub = this.conversationService.selectedConversation.subscribe(
      (conversation: Conversation | null) => {
        if (conversation) {
          this.conversationSelected.emit(conversation);
        }
      }
    );
    this.subscriptions.push(createdSub);
    this.subscriptions.push(selectedSub);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
  }

  selectConversation(conversation: Conversation): void {
    this.conversationService.selectConversation(conversation);
    this.conversationSelected.emit(conversation);
  }

  getDisplayName(conversation: Conversation): string {
    return this.conversationService.getDisplayName(conversation);
  }

  getAvatarUserId(conversation: Conversation): string {
    if (conversation.type === 'Private' && conversation.otherParticipants.length > 0) {
      return conversation.otherParticipants[0].userId;
    }
    return conversation.id.toString();
  }

  isSelected(conversation: Conversation, selected: Conversation | null): boolean {
    return selected?.id === conversation.id;
  }

  openNewChat(): void {
    this.showNewChatDialog = true;
  }

  onNewChatUserSelected(member: TeamMember): void {
    this.conversationService.createConversation(member.userId);
  }

  onNewChatClosed(): void {
    this.showNewChatDialog = false;
  }
}
