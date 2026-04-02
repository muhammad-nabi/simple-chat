import { Component } from '@angular/core';

@Component({
  selector: 'app-online-users',
  standalone: true,
  template: `
    <div class="online-users-placeholder">
      <p>Members</p>
    </div>
  `,
  styles: [`
    .online-users-placeholder {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #667781;
    }
  `],
})
export class OnlineUsersComponent {}
