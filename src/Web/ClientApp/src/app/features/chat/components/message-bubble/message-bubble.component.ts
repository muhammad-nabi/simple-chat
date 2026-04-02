import { Component, Input } from '@angular/core';
import { Message } from '../../models/message.model';
import { MessageTimestampPipe } from '../../pipes/message-timestamp.pipe';

@Component({
  selector: 'app-message-bubble',
  standalone: true,
  imports: [MessageTimestampPipe],
  templateUrl: './message-bubble.component.html',
  styleUrl: './message-bubble.component.scss',
})
export class MessageBubbleComponent {
  @Input({ required: true }) message!: Message;
  @Input() isOwn = false;
  @Input() showSender = false;
  @Input() isConsecutive = false;

  get ariaLabel(): string {
    const sender = this.isOwn ? 'You' : this.message.senderDisplayName;
    const pipe = new MessageTimestampPipe();
    const time = pipe.transform(this.message.sentAt);
    return `${sender}: ${this.message.content}, ${time}`;
  }
}
