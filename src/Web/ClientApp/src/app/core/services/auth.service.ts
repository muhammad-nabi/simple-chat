import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';

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

  private accessToken$ = new BehaviorSubject<string | null>(null);
  private user$ = new BehaviorSubject<CurrentUser | null>(null);

  readonly isAuthenticated$ = new BehaviorSubject<boolean>(false);
  readonly currentUser$: Observable<CurrentUser | null> = this.user$.asObservable();

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

  logout(): void {
    this.accessToken$.next(null);
    this.user$.next(null);
    this.isAuthenticated$.next(false);
  }

  private setSession(response: AuthResponse): void {
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
