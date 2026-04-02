import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageBubbleComponent } from './message-bubble.component';
import { MessageTimestampPipe } from '../../pipes/message-timestamp.pipe';
import { Message } from '../../models/message.model';

describe('MessageBubbleComponent', () => {
  let component: MessageBubbleComponent;
  let fixture: ComponentFixture<MessageBubbleComponent>;

  const baseMessage: Message = {
    id: 1,
    conversationId: 1,
    senderId: 'user-2',
    senderDisplayName: 'Alice',
    content: 'Hello world',
    sentAt: new Date().toISOString(),
    messageType: 'Text',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MessageBubbleComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MessageBubbleComponent);
    component = fixture.componentInstance;
    component.message = baseMessage;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should apply own class for own messages', () => {
    component.isOwn = true;
    fixture.detectChanges();
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('.message-wrapper');
    expect(wrapper.classList.contains('own')).toBe(true);
    expect(wrapper.classList.contains('other')).toBe(false);
  });

  it('should apply other class for other messages', () => {
    component.isOwn = false;
    fixture.detectChanges();
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('.message-wrapper');
    expect(wrapper.classList.contains('other')).toBe(true);
    expect(wrapper.classList.contains('own')).toBe(false);
  });

  it('should show sender name when showSender is true and not own message', () => {
    component.isOwn = false;
    component.showSender = true;
    fixture.detectChanges();
    const senderEl: HTMLElement | null = fixture.nativeElement.querySelector('.sender-name');
    expect(senderEl).toBeTruthy();
    expect(senderEl?.textContent?.trim()).toBe('Alice');
  });

  it('should not show sender name for own messages even when showSender is true', () => {
    component.isOwn = true;
    component.showSender = true;
    fixture.detectChanges();
    const senderEl: HTMLElement | null = fixture.nativeElement.querySelector('.sender-name');
    expect(senderEl).toBeFalsy();
  });

  it('should not show sender name when showSender is false', () => {
    component.isOwn = false;
    component.showSender = false;
    fixture.detectChanges();
    const senderEl: HTMLElement | null = fixture.nativeElement.querySelector('.sender-name');
    expect(senderEl).toBeFalsy();
  });

  it('should display message content', () => {
    fixture.detectChanges();
    const contentEl: HTMLElement = fixture.nativeElement.querySelector('.content');
    expect(contentEl.textContent?.trim()).toBe('Hello world');
  });

  it('should display timestamp', () => {
    fixture.detectChanges();
    const timestampEl: HTMLElement = fixture.nativeElement.querySelector('.timestamp');
    expect(timestampEl).toBeTruthy();
    expect(timestampEl.textContent?.trim()).toBeTruthy();
  });

  it('should apply consecutive class for consecutive messages', () => {
    component.isConsecutive = true;
    fixture.detectChanges();
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('.message-wrapper');
    expect(wrapper.classList.contains('consecutive')).toBe(true);
  });

  it('should not apply consecutive class by default', () => {
    fixture.detectChanges();
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('.message-wrapper');
    expect(wrapper.classList.contains('consecutive')).toBe(false);
  });

  it('should have aria-label with sender, content and time', () => {
    component.isOwn = false;
    fixture.detectChanges();
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('.message-wrapper');
    const label = wrapper.getAttribute('aria-label');
    expect(label).toContain('Alice');
    expect(label).toContain('Hello world');
  });

  it('should use "You" as sender in aria-label for own messages', () => {
    component.isOwn = true;
    fixture.detectChanges();
    const wrapper: HTMLElement = fixture.nativeElement.querySelector('.message-wrapper');
    const label = wrapper.getAttribute('aria-label');
    expect(label).toContain('You');
  });

  describe('MessageTimestampPipe', () => {
    let pipe: MessageTimestampPipe;

    beforeEach(() => {
      pipe = new MessageTimestampPipe();
    });

    it('should return HH:mm for today', () => {
      const now = new Date();
      now.setHours(14, 32, 0, 0);
      const result = pipe.transform(now.toISOString());
      expect(result).toBe('14:32');
    });

    it('should return "Yesterday HH:mm" for yesterday', () => {
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      yesterday.setHours(9, 15, 0, 0);
      const result = pipe.transform(yesterday.toISOString());
      expect(result).toMatch(/^Yesterday 09:15$/);
    });

    it('should return "Mon d, HH:mm" for same year', () => {
      const date = new Date();
      date.setMonth(2, 28); // Mar 28
      date.setHours(14, 32, 0, 0);
      // Only test if not today or yesterday
      const now = new Date();
      const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      const dateDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());
      const yesterday = new Date(today.getTime() - 86400000);

      if (dateDay.getTime() !== today.getTime() && dateDay.getTime() !== yesterday.getTime()) {
        const result = pipe.transform(date.toISOString());
        expect(result).toBe('Mar 28, 14:32');
      }
    });

    it('should include year for older dates', () => {
      const old = new Date(2024, 0, 15, 10, 30);
      const result = pipe.transform(old.toISOString());
      expect(result).toBe('Jan 15, 2024 10:30');
    });
  });
});
