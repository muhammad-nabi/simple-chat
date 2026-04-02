import { Injectable, inject, DestroyRef } from '@angular/core';
import { BehaviorSubject, Observable, Subject, skip, distinctUntilChanged } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '../services/auth.service';
import { SignalREvents, MessagePayload } from './signalr.events';

export type ConnectionState = 'Connected' | 'Reconnecting' | 'Disconnected';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private connection: signalR.HubConnection | null = null;
  private readonly connectionState$ = new BehaviorSubject<ConnectionState>('Disconnected');
  private readonly messageReceived$ = new Subject<MessagePayload>();

  readonly connectionState: Observable<ConnectionState> = this.connectionState$.asObservable();
  readonly messageReceived: Observable<MessagePayload> = this.messageReceived$.asObservable();

  constructor() {
    // Auto-start/stop based on auth state changes
    const subscription = this.authService.isAuthenticated$.pipe(
      skip(1), // Skip initial value — APP_INITIALIZER handles first auth
      distinctUntilChanged(),
    ).subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.start();
      } else {
        this.stop();
      }
    });

    this.destroyRef.onDestroy(() => subscription.unsubscribe());
  }

  start(): void {
    if (this.connection) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => this.authService.accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.registerEventHandlers();
    this.registerLifecycleHandlers();

    this.connection.start()
      .then(() => this.connectionState$.next('Connected'))
      .catch(err => {
        console.error('SignalR connection error:', err);
        this.connection = null;
      });
  }

  stop(): void {
    if (!this.connection) {
      return;
    }

    this.connection.stop()
      .then(() => {
        this.connection = null;
        this.connectionState$.next('Disconnected');
      })
      .catch(err => console.error('SignalR stop error:', err));
  }

  async sendMessage(conversationId: number, content: string): Promise<number> {
    if (!this.connection) {
      throw new Error('SignalR connection not established.');
    }
    return await this.connection.invoke<number>('SendMessage', conversationId, content);
  }

  async joinConversation(conversationId: number): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not established.');
    }
    await this.connection.invoke('JoinConversation', conversationId);
  }

  private registerEventHandlers(): void {
    if (!this.connection) {
      return;
    }

    this.connection.on(SignalREvents.ReceiveMessage, (message: MessagePayload) => {
      this.messageReceived$.next(message);
    });

    // Register listeners for future use (Epic 5: Presence) — no-op for now
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    this.connection.on(SignalREvents.UserOnline, () => {});
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    this.connection.on(SignalREvents.UserOffline, () => {});
  }

  private registerLifecycleHandlers(): void {
    if (!this.connection) {
      return;
    }

    this.connection.onreconnecting(() => {
      this.connectionState$.next('Reconnecting');
    });

    this.connection.onreconnected(() => {
      this.connectionState$.next('Connected');
    });

    this.connection.onclose(() => {
      this.connection = null;
      this.connectionState$.next('Disconnected');
    });
  }
}
