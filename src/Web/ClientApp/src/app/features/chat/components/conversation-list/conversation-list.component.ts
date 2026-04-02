import { Component } from '@angular/core';

@Component({
  selector: 'app-conversation-list',
  standalone: true,
  template: `
    <div class="conversation-list-placeholder">
      <p>Conversations</p>
    </div>
  `,
  styles: [`
    .conversation-list-placeholder {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #667781;
    }
  `],
})
export class ConversationListComponent {}
