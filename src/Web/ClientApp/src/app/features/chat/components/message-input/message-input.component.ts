import {
  Component,
  OnInit,
  OnDestroy,
  Input,
  ViewChild,
  ElementRef,
  AfterViewInit,
  inject,
} from '@angular/core';
import { Subscription } from 'rxjs';
import { pairwise, startWith } from 'rxjs/operators';
import { MessageService } from '../../services/message.service';
import { DraftService } from '../../services/draft.service';
import { ConversationService } from '../../services/conversation.service';
import { Conversation } from '../../models/conversation.model';
import { FormsModule } from '@angular/forms';
import { LayoutMode } from '../chat-layout/chat-layout.component';

@Component({
  selector: 'app-message-input',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './message-input.component.html',
  styleUrl: './message-input.component.scss',
})
export class MessageInputComponent implements OnInit, OnDestroy, AfterViewInit {
  private readonly messageService = inject(MessageService);
  private readonly draftService = inject(DraftService);
  private readonly conversationService = inject(ConversationService);

  @Input() layoutMode: LayoutMode = 'desktop';

  @ViewChild('messageTextarea') messageTextarea!: ElementRef<HTMLTextAreaElement>;

  messageText = '';
  showDraftIndicator = false;
  sendError: string | null = null;
  sending = false;

  private currentConversationId: number | null = null;
  private subscriptions: Subscription[] = [];
  private isFirstKeystrokeAfterDraftRestore = false;
  private lastSentContent: string | null = null;
  private sendDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    const convSub = this.conversationService.selectedConversation.pipe(
      startWith(null as Conversation | null),
      pairwise(),
    ).subscribe(([prev, next]: [Conversation | null, Conversation | null]) => {
      // Save draft for previous conversation
      if (prev && this.messageText.trim()) {
        this.draftService.saveDraft(prev.id, this.messageText);
      } else if (prev) {
        this.draftService.clearDraft(prev.id);
      }

      // Load draft for new conversation
      if (next) {
        this.currentConversationId = next.id;
        const draft = this.draftService.loadDraft(next.id);
        if (draft) {
          this.messageText = draft;
          this.showDraftIndicator = true;
          this.isFirstKeystrokeAfterDraftRestore = true;
        } else {
          this.messageText = '';
          this.showDraftIndicator = false;
        }
        this.sendError = null;
        this.autoFocusIfDesktop();
      } else {
        this.currentConversationId = null;
        this.messageText = '';
        this.showDraftIndicator = false;
      }
    });

    const errorSub = this.messageService.sendError$.subscribe(
      (error: string | null) => {
        this.sendError = error;
        if (error && this.lastSentContent) {
          this.messageText = this.lastSentContent;
        }
      }
    );

    const confirmSub = this.messageService.sendConfirmed$.subscribe(
      (conversationId: number) => {
        if (conversationId === this.currentConversationId) {
          this.draftService.clearDraft(conversationId);
          this.lastSentContent = null;
        }
      }
    );

    this.subscriptions.push(convSub, errorSub, confirmSub);
  }

  ngAfterViewInit(): void {
    this.autoFocusIfDesktop();
  }

  ngOnDestroy(): void {
    // Save draft on destroy
    if (this.currentConversationId && this.messageText.trim()) {
      this.draftService.saveDraft(this.currentConversationId, this.messageText);
    }
    if (this.sendDebounceTimer) {
      clearTimeout(this.sendDebounceTimer);
    }
    this.subscriptions.forEach(s => s.unsubscribe());
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  onInput(): void {
    if (this.isFirstKeystrokeAfterDraftRestore) {
      this.isFirstKeystrokeAfterDraftRestore = false;
      this.showDraftIndicator = false;
    }
  }

  send(): void {
    const content = this.messageText.trim();
    if (!content || !this.currentConversationId || this.sending) {
      return;
    }

    if (content.length > 4000) {
      return;
    }

    this.sending = true;
    this.lastSentContent = content;
    this.messageService.clearSendError();
    this.messageService.sendMessage(this.currentConversationId, content);
    this.messageText = '';
    this.showDraftIndicator = false;

    // Re-enable send after debounce
    this.sendDebounceTimer = setTimeout(() => {
      this.sending = false;
    }, 300);

    // Return focus to textarea
    this.messageTextarea?.nativeElement?.focus();
  }

  retry(): void {
    this.messageService.clearSendError();
    this.sending = false;
    if (!this.messageText.trim() && this.lastSentContent) {
      this.messageText = this.lastSentContent;
    }
    this.send();
  }

  get isOverLimit(): boolean {
    return this.messageText.length > 4000;
  }

  get canSend(): boolean {
    return this.messageText.trim().length > 0
      && !this.isOverLimit
      && !this.sending
      && this.currentConversationId !== null;
  }

  private autoFocusIfDesktop(): void {
    if (this.layoutMode === 'desktop' && this.messageTextarea) {
      setTimeout(() => this.messageTextarea.nativeElement.focus(), 0);
    }
  }
}
