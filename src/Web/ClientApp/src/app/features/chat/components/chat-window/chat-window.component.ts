import { Component } from '@angular/core';

@Component({
  selector: 'app-chat-window',
  standalone: true,
  template: `
    <div class="chat-window-placeholder">
      <p>Select a conversation</p>
    </div>
  `,
  styles: [`
    .chat-window-placeholder {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #667781;
    }
  `],
})
export class ChatWindowComponent {}
