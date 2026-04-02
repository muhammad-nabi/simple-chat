import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-unread-badge',
  standalone: true,
  template: `
    @if (count() > 0) {
      <span
        class="unread-badge"
        [attr.aria-label]="ariaLabel()">
        {{ displayText() }}
      </span>
    }
  `,
  styles: [`
    .unread-badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 22px;
      height: 22px;
      padding: 0 6px;
      border-radius: 11px;
      background-color: #128C7E;
      color: #ffffff;
      font-size: 12px;
      font-weight: 600;
      line-height: 1;
      flex-shrink: 0;
    }
  `],
})
export class UnreadBadgeComponent {
  readonly count = input.required<number>();

  readonly displayText = computed(() => {
    const c = this.count();
    return c > 99 ? '99+' : c.toString();
  });

  readonly ariaLabel = computed(() => {
    const c = this.count();
    return `${c} unread message${c === 1 ? '' : 's'}`;
  });
}
