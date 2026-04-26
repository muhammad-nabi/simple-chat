import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PresenceService } from './core/presence/presence.service';

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
export class AppComponent {
  // Inject to trigger singleton instantiation — PresenceService self-manages via auth state
  private readonly _presence = inject(PresenceService);
}
