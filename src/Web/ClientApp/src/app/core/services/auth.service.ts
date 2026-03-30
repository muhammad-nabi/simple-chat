import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, tap, catchError, shareReplay, finalize, map } from 'rxjs';

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  userId: string;
  displayName: string;
  email: string;
  role: string;
}

export interface CurrentUser {
  userId: string;
  displayName: string;
  email: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private accessToken$ = new BehaviorSubject<string | null>(null);
  private user$ = new BehaviorSubject<CurrentUser | null>(null);

  readonly isAuthenticated$ = new BehaviorSubject<boolean>(false);
  readonly currentUser$: Observable<CurrentUser | null> = this.user$.asObservable();

  /** Shared observable for in-flight refresh — prevents thundering herd */
  private refreshInFlight$: Observable<AuthResponse> | null = null;

  /** Flag to prevent interceptor from retrying 401 during logout */
  isLoggingOut = false;

  get accessToken(): string | null {
    return this.accessToken$.value;
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/register', request, { withCredentials: true })
      .pipe(
        tap(response => {
          this.setSession(response);
        }),
      );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/login', request, { withCredentials: true })
      .pipe(
        tap(response => {
          this.setSession(response);
        }),
      );
  }

  /**
   * Refresh the access token using the HttpOnly refresh token cookie.
   * Returns a shared observable — concurrent callers get the same in-flight request.
   */
  refresh(): Observable<AuthResponse> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    this.refreshInFlight$ = this.http
      .post<AuthResponse>('/api/auth/refresh', {}, { withCredentials: true })
      .pipe(
        tap(response => {
          this.setSession(response);
        }),
        shareReplay(1),
        finalize(() => {
          this.refreshInFlight$ = null;
        }),
      );

    return this.refreshInFlight$;
  }

  /**
   * Called by APP_INITIALIZER on app startup.
   * Attempts to restore session from refresh token cookie.
   * Returns Observable<boolean> — true if session restored, false otherwise.
   * NEVER throws — a failed refresh is normal (user not logged in).
   */
  initializeAuth(): Observable<boolean> {
    return this.refresh().pipe(
      map(() => true),
      catchError(() => of(false)),
    );
  }

  logout(): void {
    if (this.isLoggingOut) return;
    this.isLoggingOut = true;
    this.refreshInFlight$ = null;

    // Best-effort server-side session invalidation
    this.http
      .post('/api/auth/logout', {}, { withCredentials: true })
      .pipe(
        catchError(() => of(null)),
        finalize(() => {
          this.isLoggingOut = false;
        }),
      )
      .subscribe(() => {
        this.clearSession();
      });
  }

  private clearSession(): void {
    this.accessToken$.next(null);
    this.user$.next(null);
    this.isAuthenticated$.next(false);
    this.router.navigate(['/login']);
  }

  private setSession(response: AuthResponse): void {
    if (this.isLoggingOut) return;
    this.accessToken$.next(response.accessToken);
    this.user$.next({
      userId: response.userId,
      displayName: response.displayName,
      email: response.email,
      role: response.role,
    });
    this.isAuthenticated$.next(true);
  }
}
