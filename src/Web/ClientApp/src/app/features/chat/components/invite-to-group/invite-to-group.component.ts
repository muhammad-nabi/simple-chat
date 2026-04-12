import { Component, EventEmitter, Input, OnInit, OnDestroy, Output, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, Observable, Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';
import { ConversationService } from '../../services/conversation.service';
import { TeamMember } from '../../models/user.model';
import { AvatarComponent } from '../../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-invite-to-group',
  standalone: true,
  imports: [AsyncPipe, FormsModule, AvatarComponent],
  templateUrl: './invite-to-group.component.html',
  styleUrl: './invite-to-group.component.scss',
})
export class InviteToGroupComponent implements OnInit, OnDestroy {
  private readonly userService = inject(UserService);
  private readonly conversationService = inject(ConversationService);

  @Input() conversationId: number | null = null;
  @Input() existingMemberIds: string[] = [];
  @Output() readonly invited = new EventEmitter<string[]>();
  @Output() readonly closed = new EventEmitter<void>();

  filterText = '';
  selectedUserIds: string[] = [];

  private readonly _availableUsers$ = new BehaviorSubject<TeamMember[]>([]);
  private readonly _inviting$ = new BehaviorSubject<boolean>(false);

  readonly availableUsers$: Observable<TeamMember[]> = this._availableUsers$.asObservable();
  readonly inviting$: Observable<boolean> = this._inviting$.asObservable();
  readonly loading$ = this.userService.loading$;

  private subscriptions: Subscription[] = [];

  ngOnInit(): void {
    this.userService.loadTeamMembers();
    const sub = this.userService.teamMembers$.subscribe((members: TeamMember[]) => {
      const filtered = members.filter(m => !this.existingMemberIds.includes(m.userId));
      this._availableUsers$.next(filtered);
    });
    this.subscriptions.push(sub);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
  }

  get filteredUsers(): TeamMember[] {
    const users = this._availableUsers$.value;
    if (!this.filterText.trim()) {
      return users;
    }
    const lower = this.filterText.toLowerCase();
    return users.filter(u => u.displayName.toLowerCase().includes(lower));
  }

  isSelected(userId: string): boolean {
    return this.selectedUserIds.includes(userId);
  }

  toggleUser(userId: string): void {
    if (this.isSelected(userId)) {
      this.selectedUserIds = this.selectedUserIds.filter(id => id !== userId);
    } else {
      this.selectedUserIds = [...this.selectedUserIds, userId];
    }
  }

  onInvite(): void {
    if (!this.conversationId || this.selectedUserIds.length === 0) {
      return;
    }
    this._inviting$.next(true);
    this.conversationService.inviteToGroup(this.conversationId, this.selectedUserIds).subscribe({
      next: () => {
        this._inviting$.next(false);
        this.invited.emit(this.selectedUserIds);
      },
      error: () => {
        this._inviting$.next(false);
      },
    });
  }

  onCancel(): void {
    this.closed.emit();
  }
}
