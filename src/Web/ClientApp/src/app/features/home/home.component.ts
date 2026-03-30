import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  standalone: true,
  template: `
    <div class="welcome">
      <h1>Welcome!</h1>
      <p>Join a conversation to get started.</p>
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
    p { font-size: 15px; color: #667781; }
  `],
})
export class HomeComponent {}
