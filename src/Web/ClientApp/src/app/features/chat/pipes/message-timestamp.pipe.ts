import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'messageTimestamp',
  standalone: true,
  pure: true,
})
export class MessageTimestampPipe implements PipeTransform {
  transform(sentAt: string): string {
    const date = new Date(sentAt);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const yesterday = new Date(today.getTime() - 86400000);
    const messageDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());

    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const time = `${hours}:${minutes}`;

    if (messageDate.getTime() === today.getTime()) {
      return time;
    }
    if (messageDate.getTime() === yesterday.getTime()) {
      return `Yesterday ${time}`;
    }
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const month = months[date.getMonth()];
    const day = date.getDate();
    if (date.getFullYear() === now.getFullYear()) {
      return `${month} ${day}, ${time}`;
    }
    return `${month} ${day}, ${date.getFullYear()} ${time}`;
  }
}
