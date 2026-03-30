import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  template: `
    <div class="welcome">
      <h1>Welcome!</h1>
      <p>Join a conversation to get started.</p>
      <button class="logout-btn" (click)="onLogout()">Log out</button>
    </div>
  `,
  styles: [`
    .welcome {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 60vh;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      color: #111B21;
    }
    h1 { font-size: 18px; font-weight: 600; margin-bottom: 8px; }
    p { font-size: 15px; color: #667781; margin-bottom: 24px; }
    .logout-btn {
      padding: 8px 20px;
      font-size: 14px;
      border: 1px solid #d1d5db;
      border-radius: 8px;
      background: #fff;
      color: #111B21;
      cursor: pointer;
    }
    .logout-btn:hover { background: #f3f4f6; }
  `],
})
export class HomeComponent {
  private authService = inject(AuthService);

  onLogout(): void {
    this.authService.logout();
  }
}
