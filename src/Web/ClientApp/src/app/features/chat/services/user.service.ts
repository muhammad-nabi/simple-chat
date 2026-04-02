import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { TeamMember } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  private readonly _teamMembers$ = new BehaviorSubject<TeamMember[]>([]);
  private readonly _loading$ = new BehaviorSubject<boolean>(false);
  private readonly _error$ = new BehaviorSubject<string | null>(null);

  readonly teamMembers$: Observable<TeamMember[]> = this._teamMembers$.asObservable();
  readonly loading$: Observable<boolean> = this._loading$.asObservable();
  readonly error$: Observable<string | null> = this._error$.asObservable();

  loadTeamMembers(): void {
    if (this._loading$.value) {
      return;
    }
    this._loading$.next(true);
    this._error$.next(null);
    this.http.get<TeamMember[]>('/api/users/team-members').subscribe({
      next: (members: TeamMember[]) => {
        this._teamMembers$.next(members);
        this._loading$.next(false);
      },
      error: () => {
        this._error$.next('Failed to load team members');
        this._loading$.next(false);
      },
    });
  }
}
