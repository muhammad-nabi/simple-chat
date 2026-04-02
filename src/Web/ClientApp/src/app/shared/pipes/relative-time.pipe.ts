import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'relativeTime',
  standalone: true,
})
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    const now = new Date();
    const diffMs = Math.max(0, now.getTime() - date.getTime());
    const diffMinutes = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);

    if (diffMinutes < 1) {
      return 'just now';
    }

    if (diffMinutes < 60) {
      return `${diffMinutes}m`;
    }

    if (diffHours < 24) {
      return `${diffHours}h`;
    }

    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const yesterday = new Date(today.getTime() - 86400000);
    const dateDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());

    if (dateDay.getTime() === yesterday.getTime()) {
      return 'Yesterday';
    }

    const weekAgo = new Date(today.getTime() - 6 * 86400000);
    if (dateDay.getTime() >= weekAgo.getTime()) {
      const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
      return days[date.getDay()];
    }

    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return `${months[date.getMonth()]} ${date.getDate()}`;
  }
}
