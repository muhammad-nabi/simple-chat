import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <a class="skip-to-content" href="#main-content">Skip to content</a>
    <main id="main-content">
      <router-outlet />
    </main>
  `,
})
export class AppComponent {}
