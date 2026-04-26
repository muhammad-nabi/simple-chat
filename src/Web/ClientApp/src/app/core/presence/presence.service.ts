import { Injectable, inject, DestroyRef, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { OnlineUser } from './presence.model';

@Injectable({ providedIn: 'root' })
export class PresenceService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly ngZone = inject(NgZone);

  private readonly _onlineUsers$ = new BehaviorSubject<OnlineUser[]>([]);
  readonly onlineUsers$: Observable<OnlineUser[]> = this._onlineUsers$.asObservable();

  private heartbeatInterval: ReturnType<typeof setInterval> | null = null;
  private pollInterval: ReturnType<typeof setInterval> | null = null;
  private isAway = false;
  private lastActivity = Date.now();
  private activityListenersBound = false;

  private readonly HEARTBEAT_MS = 60000;
  private readonly POLL_MS = 30000;
  private readonly IDLE_THRESHOLD_MS = 300000; // 5 minutes

  private readonly onActivity = (): void => {
    this.lastActivity = Date.now();
    if (this.isAway) {
      this.isAway = false;
      this.sendHeartbeat();
    }
  };

  constructor() {
    const subscription = this.authService.isAuthenticated$.pipe(
      distinctUntilChanged(),
    ).subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.start();
      } else {
        this.stop();
      }
    });

    this.destroyRef.onDestroy(() => {
      subscription.unsubscribe();
      this.stop();
    });
  }

  private start(): void {
    if (this.heartbeatInterval !== null) {
      return;
    }

    this.lastActivity = Date.now();
    this.isAway = false;
    this.bindActivityListeners();

    // Immediate first heartbeat and poll
    this.sendHeartbeat();
    this.pollOnlineUsers();

    // Run intervals outside Angular zone to avoid unnecessary change detection
    this.ngZone.runOutsideAngular(() => {
      this.heartbeatInterval = setInterval(() => this.sendHeartbeat(), this.HEARTBEAT_MS);
      this.pollInterval = setInterval(() => this.pollOnlineUsers(), this.POLL_MS);
    });
  }

  private stop(): void {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = null;
    }
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
    this.unbindActivityListeners();
    this._onlineUsers$.next([]);
  }

  private sendHeartbeat(): void {
    const now = Date.now();
    if (now - this.lastActivity > this.IDLE_THRESHOLD_MS) {
      this.isAway = true;
    }

    const status = this.isAway ? 'Away' : 'Online';

    this.http.post('/api/presence/heartbeat', { status }).subscribe({
      error: (err: unknown) => console.error('Heartbeat failed:', err),
    });
  }

  private pollOnlineUsers(): void {
    this.http.get<OnlineUser[]>('/api/presence/online').subscribe({
      next: (users: OnlineUser[]) => {
        this.ngZone.run(() => this._onlineUsers$.next(users));
      },
      error: (err: unknown) => console.error('Poll online users failed:', err),
    });
  }

  private bindActivityListeners(): void {
    if (this.activityListenersBound) {
      return;
    }
    document.addEventListener('mousemove', this.onActivity, { passive: true });
    document.addEventListener('keydown', this.onActivity, { passive: true });
    document.addEventListener('touchstart', this.onActivity, { passive: true });
    document.addEventListener('click', this.onActivity, { passive: true });
    this.activityListenersBound = true;
  }

  private unbindActivityListeners(): void {
    document.removeEventListener('mousemove', this.onActivity);
    document.removeEventListener('keydown', this.onActivity);
    document.removeEventListener('touchstart', this.onActivity);
    document.removeEventListener('click', this.onActivity);
    this.activityListenersBound = false;
  }
}
