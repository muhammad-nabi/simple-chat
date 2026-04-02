import { Component, OnInit, OnDestroy } from '@angular/core';
import { inject } from '@angular/core';
import { Subscription, switchMap, of, timer, map } from 'rxjs';
import { SignalRService, ConnectionState } from '../../../core/signalr/signalr.service';

@Component({
  selector: 'app-connection-status-banner',
  standalone: true,
  templateUrl: './connection-status-banner.component.html',
  styleUrl: './connection-status-banner.component.scss',
})
export class ConnectionStatusBannerComponent implements OnInit, OnDestroy {
  private readonly signalRService = inject(SignalRService);
  private subscription: Subscription | null = null;

  visibleState: ConnectionState | null = null;

  ngOnInit(): void {
    this.subscription = this.signalRService.connectionState.pipe(
      switchMap((state: ConnectionState) => {
        if (state === 'Connected') {
          return of(null);
        }
        if (state === 'Disconnected') {
          return of(state as ConnectionState | null);
        }
        // Reconnecting: delay 3s, cancel if state changes before timer fires
        return timer(3000).pipe(map(() => state as ConnectionState | null));
      }),
    ).subscribe((state: ConnectionState | null) => {
      this.visibleState = state;
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }
}
