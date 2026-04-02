import { Component, inject, OnInit, output } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ConversationService } from '../../services/conversation.service';
import { Conversation } from '../../models/conversation.model';
import { AvatarComponent } from '../../../../shared/components/avatar/avatar.component';
import { UnreadBadgeComponent } from '../../../../shared/components/unread-badge/unread-badge.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';

@Component({
  selector: 'app-conversation-list',
  standalone: true,
  imports: [AsyncPipe, AvatarComponent, UnreadBadgeComponent, RelativeTimePipe],
  templateUrl: './conversation-list.component.html',
  styleUrl: './conversation-list.component.scss',
})
export class ConversationListComponent implements OnInit {
  private readonly conversationService = inject(ConversationService);

  readonly conversations$ = this.conversationService.conversations;
  readonly selectedConversation$ = this.conversationService.selectedConversation;
  readonly loading$ = this.conversationService.loading;

  readonly conversationSelected = output<Conversation>();

  ngOnInit(): void {
    this.conversationService.loadConversations();
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
}
