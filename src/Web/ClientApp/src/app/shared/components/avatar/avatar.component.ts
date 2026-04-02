import { Component, computed, input } from '@angular/core';

const AVATAR_COLORS = [
  '#E57373', '#F06292', '#BA68C8', '#7986CB',
  '#4FC3F7', '#4DB6AC', '#FFD54F', '#FF8A65',
];

const SIZE_MAP: Record<string, number> = {
  sidebar: 44,
  header: 36,
  members: 30,
};

@Component({
  selector: 'app-avatar',
  standalone: true,
  template: `
    <div
      class="avatar"
      [style.width.px]="sizeInPx()"
      [style.height.px]="sizeInPx()"
      [style.backgroundColor]="backgroundColor()"
      [style.fontSize.px]="fontSize()"
      [attr.aria-label]="ariaLabel()">
      {{ initials() }}
    </div>
  `,
  styles: [`
    .avatar {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border-radius: 14px;
      color: #ffffff;
      font-weight: 600;
      line-height: 1;
      flex-shrink: 0;
      user-select: none;
    }
  `],
})
export class AvatarComponent {
  readonly displayName = input.required<string>();
  readonly userId = input.required<string>();
  readonly size = input<'sidebar' | 'header' | 'members'>('sidebar');
  readonly presenceStatus = input<string | undefined>(undefined);

  readonly initials = computed(() => {
    const name = (this.displayName() ?? '').trim();
    if (!name) {
      return '?';
    }
    const words = name.split(/\s+/).filter(w => w.length > 0);
    if (words.length >= 2) {
      return (words[0][0] + words[1][0]).toUpperCase();
    }
    return words.length > 0 ? words[0][0].toUpperCase() : '?';
  });

  readonly backgroundColor = computed(() => {
    const id = this.userId();
    const index = id.charCodeAt(0) % AVATAR_COLORS.length;
    return AVATAR_COLORS[index];
  });

  readonly sizeInPx = computed(() => SIZE_MAP[this.size()] ?? 44);

  readonly fontSize = computed(() => {
    const px = this.sizeInPx();
    return Math.round(px * 0.38);
  });

  readonly ariaLabel = computed(() => this.displayName());
}
