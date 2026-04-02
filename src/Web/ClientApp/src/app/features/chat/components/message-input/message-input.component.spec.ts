import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, Subject } from 'rxjs';
import { MessageInputComponent } from './message-input.component';
import { MessageService } from '../../services/message.service';
import { DraftService } from '../../services/draft.service';
import { ConversationService } from '../../services/conversation.service';
import { Conversation } from '../../models/conversation.model';

describe('MessageInputComponent', () => {
  let component: MessageInputComponent;
  let fixture: ComponentFixture<MessageInputComponent>;
  let selectedConversation$: BehaviorSubject<Conversation | null>;
  let sendError$: BehaviorSubject<string | null>;
  let sendConfirmed$: Subject<number>;
  let mockMessageService: {
    sendMessage: jest.Mock;
    clearSendError: jest.Mock;
    sendError$: BehaviorSubject<string | null>;
    sendConfirmed$: Subject<number>;
    isFirstMessageInConversation: jest.Mock;
  };
  let mockDraftService: {
    saveDraft: jest.Mock;
    loadDraft: jest.Mock;
    clearDraft: jest.Mock;
    draft$: BehaviorSubject<string | null>;
  };

  const testConversation: Conversation = {
    id: 1,
    type: 'Private',
    name: null,
    lastMessagePreview: null,
    lastMessageAt: null,
    otherParticipants: [{ userId: 'user-2', displayName: 'Alice' }],
    unreadCount: 0,
  };

  const testConversation2: Conversation = {
    id: 2,
    type: 'Private',
    name: null,
    lastMessagePreview: null,
    lastMessageAt: null,
    otherParticipants: [{ userId: 'user-3', displayName: 'Bob' }],
    unreadCount: 0,
  };

  beforeEach(async () => {
    selectedConversation$ = new BehaviorSubject<Conversation | null>(null);
    sendError$ = new BehaviorSubject<string | null>(null);
    sendConfirmed$ = new Subject<number>();

    mockMessageService = {
      sendMessage: jest.fn().mockReturnValue({ tempId: -1 }),
      clearSendError: jest.fn(),
      sendError$,
      sendConfirmed$,
      isFirstMessageInConversation: jest.fn().mockReturnValue(true),
    };

    mockDraftService = {
      saveDraft: jest.fn(),
      loadDraft: jest.fn().mockReturnValue(null),
      clearDraft: jest.fn(),
      draft$: new BehaviorSubject<string | null>(null),
    };

    await TestBed.configureTestingModule({
      imports: [MessageInputComponent, FormsModule],
      providers: [
        { provide: MessageService, useValue: mockMessageService },
        { provide: DraftService, useValue: mockDraftService },
        { provide: ConversationService, useValue: { selectedConversation: selectedConversation$.asObservable() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MessageInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('AC 1: Message Input Component UI', () => {
    it('should render textarea with aria-label', () => {
      const textarea: HTMLTextAreaElement = fixture.nativeElement.querySelector('.message-textarea');
      expect(textarea).toBeTruthy();
      expect(textarea.getAttribute('aria-label')).toBe('Type a message');
    });

    it('should render disabled attach button', () => {
      const button: HTMLButtonElement = fixture.nativeElement.querySelector('.attach-button');
      expect(button).toBeTruthy();
      expect(button.disabled).toBe(true);
      expect(button.getAttribute('aria-label')).toBe('Attach file (coming soon)');
    });

    it('should render send button with aria-label', () => {
      const button: HTMLButtonElement = fixture.nativeElement.querySelector('.send-button');
      expect(button).toBeTruthy();
      expect(button.getAttribute('aria-label')).toBe('Send message');
    });

    it('should disable send button when textarea is empty', () => {
      const button: HTMLButtonElement = fixture.nativeElement.querySelector('.send-button');
      expect(button.disabled).toBe(true);
    });
  });

  describe('AC 2: Message Submission', () => {
    beforeEach(() => {
      selectedConversation$.next(testConversation);
      fixture.detectChanges();
    });

    it('should call sendMessage when send button is clicked', () => {
      component.messageText = 'Hello';
      fixture.detectChanges();

      component.send();

      expect(mockMessageService.sendMessage).toHaveBeenCalledWith(1, 'Hello');
    });

    it('should clear textarea after sending', () => {
      component.messageText = 'Hello';
      component.send();

      expect(component.messageText).toBe('');
    });

    it('should not send empty messages', () => {
      component.messageText = '   ';
      component.send();

      expect(mockMessageService.sendMessage).not.toHaveBeenCalled();
    });

    it('should not send messages over 4000 chars', () => {
      component.messageText = 'a'.repeat(4001);
      component.send();

      expect(mockMessageService.sendMessage).not.toHaveBeenCalled();
    });

    it('should debounce send for 300ms', fakeAsync(() => {
      component.messageText = 'Hello';
      component.send();

      expect(component.sending).toBe(true);

      component.messageText = 'Hello again';
      component.send();
      expect(mockMessageService.sendMessage).toHaveBeenCalledTimes(1);

      tick(300);
      expect(component.sending).toBe(false);
    }));

    it('should clear draft after server confirmation', () => {
      component.messageText = 'Hello';
      component.send();

      // Draft not cleared immediately
      expect(mockDraftService.clearDraft).not.toHaveBeenCalled();

      // Simulate server confirmation
      sendConfirmed$.next(1);

      expect(mockDraftService.clearDraft).toHaveBeenCalledWith(1);
    });
  });

  describe('AC 3: Multiline Input Support', () => {
    it('should send on Enter without Shift', () => {
      selectedConversation$.next(testConversation);
      fixture.detectChanges();
      component.messageText = 'Hello';

      const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: false });
      jest.spyOn(event, 'preventDefault');
      component.onKeydown(event);

      expect(event.preventDefault).toHaveBeenCalled();
      expect(mockMessageService.sendMessage).toHaveBeenCalled();
    });

    it('should not send on Shift+Enter', () => {
      selectedConversation$.next(testConversation);
      fixture.detectChanges();
      component.messageText = 'Hello';

      const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true });
      jest.spyOn(event, 'preventDefault');
      component.onKeydown(event);

      expect(event.preventDefault).not.toHaveBeenCalled();
      expect(mockMessageService.sendMessage).not.toHaveBeenCalled();
    });
  });

  describe('AC 4: Draft Persistence & Restoration', () => {
    it('should save draft when switching conversations', () => {
      selectedConversation$.next(testConversation);
      fixture.detectChanges();
      component.messageText = 'My draft';

      selectedConversation$.next(testConversation2);
      fixture.detectChanges();

      expect(mockDraftService.saveDraft).toHaveBeenCalledWith(1, 'My draft');
    });

    it('should load draft when switching to conversation with saved draft', () => {
      mockDraftService.loadDraft.mockReturnValue('Saved draft text');

      selectedConversation$.next(testConversation);
      fixture.detectChanges();

      expect(component.messageText).toBe('Saved draft text');
      expect(component.showDraftIndicator).toBe(true);
    });

    it('should clear draft indicator on first keystroke', () => {
      mockDraftService.loadDraft.mockReturnValue('Draft');
      selectedConversation$.next(testConversation);
      fixture.detectChanges();

      expect(component.showDraftIndicator).toBe(true);

      component.onInput();

      expect(component.showDraftIndicator).toBe(false);
    });

    it('should show draft indicator in the DOM', () => {
      mockDraftService.loadDraft.mockReturnValue('Draft');
      selectedConversation$.next(testConversation);
      fixture.detectChanges();

      const indicator: HTMLElement = fixture.nativeElement.querySelector('.draft-indicator');
      expect(indicator).toBeTruthy();
      expect(indicator.textContent?.trim()).toBe('Draft');
      expect(indicator.getAttribute('aria-live')).toBe('polite');
    });

    it('should clear empty draft when switching', () => {
      selectedConversation$.next(testConversation);
      fixture.detectChanges();
      component.messageText = '';

      selectedConversation$.next(testConversation2);
      fixture.detectChanges();

      expect(mockDraftService.clearDraft).toHaveBeenCalledWith(1);
    });
  });

  describe('AC 6: Offline Error Handling', () => {
    it('should display send error', () => {
      sendError$.next('Couldn\'t send \u2014 tap to retry');
      fixture.detectChanges();

      const errorEl: HTMLElement = fixture.nativeElement.querySelector('.send-error');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent?.trim()).toBe('Couldn\'t send \u2014 tap to retry');
      expect(errorEl.getAttribute('role')).toBe('alert');
      expect(errorEl.getAttribute('aria-live')).toBe('assertive');
    });

    it('should not display error when no error', () => {
      sendError$.next(null);
      fixture.detectChanges();

      const errorEl: HTMLElement | null = fixture.nativeElement.querySelector('.send-error');
      expect(errorEl).toBeFalsy();
    });

    it('should restore message text on send error', () => {
      selectedConversation$.next(testConversation);
      fixture.detectChanges();
      component.messageText = 'Failed message';
      component.send();

      expect(component.messageText).toBe('');

      sendError$.next('Couldn\'t send \u2014 tap to retry');

      expect(component.messageText).toBe('Failed message');
    });

    it('should retry on error click with restored text', () => {
      selectedConversation$.next(testConversation);
      fixture.detectChanges();
      component.messageText = 'Retry text';
      component.send();

      sendError$.next('Error');

      mockMessageService.sendMessage.mockClear();
      component.retry();

      expect(mockMessageService.clearSendError).toHaveBeenCalled();
      expect(mockMessageService.sendMessage).toHaveBeenCalledWith(1, 'Retry text');
    });
  });

  describe('character limit', () => {
    it('should show character limit error when over 4000', () => {
      component.messageText = 'a'.repeat(4001);
      fixture.detectChanges();

      const errorEl: HTMLElement = fixture.nativeElement.querySelector('.char-error');
      expect(errorEl).toBeTruthy();
    });

    it('should not show character limit error when under 4000', () => {
      component.messageText = 'Hello';
      fixture.detectChanges();

      const errorEl: HTMLElement | null = fixture.nativeElement.querySelector('.char-error');
      expect(errorEl).toBeFalsy();
    });
  });

  describe('auto-focus', () => {
    it('should have layoutMode input defaulting to desktop', () => {
      expect(component.layoutMode).toBe('desktop');
    });
  });
});
