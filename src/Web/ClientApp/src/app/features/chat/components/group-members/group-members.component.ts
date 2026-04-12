import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { BehaviorSubject, Observable } from 'rxjs';
import { ConversationService, GroupMemberDto } from '../../services/conversation.service';
import { AvatarComponent } from '../../../../shared/components/avatar/avatar.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';

@Component({
  selector: 'app-group-members',
  standalone: true,
  imports: [AsyncPipe, AvatarComponent, RelativeTimePipe],
  templateUrl: './group-members.component.html',
  styleUrl: './group-members.component.scss',
})
export class GroupMembersComponent implements OnChanges {
  private readonly conversationService = inject(ConversationService);

  @Input() conversationId: number | null = null;
  @Output() readonly inviteRequested = new EventEmitter<void>();
  @Output() readonly leftGroup = new EventEmitter<void>();
  @Output() readonly closed = new EventEmitter<void>();

  private readonly _members$ = new BehaviorSubject<GroupMemberDto[]>([]);
  private readonly _loading$ = new BehaviorSubject<boolean>(false);
  private readonly _leaving$ = new BehaviorSubject<boolean>(false);

  readonly members$: Observable<GroupMemberDto[]> = this._members$.asObservable();
  readonly loading$: Observable<boolean> = this._loading$.asObservable();
  readonly leaving$: Observable<boolean> = this._leaving$.asObservable();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['conversationId'] && this.conversationId) {
      this.loadMembers();
    }
  }

  loadMembers(): void {
    if (!this.conversationId) {
      return;
    }
    this._loading$.next(true);
    this.conversationService.getGroupMembers(this.conversationId).subscribe({
      next: (members: GroupMemberDto[]) => {
        this._members$.next(members);
        this._loading$.next(false);
      },
      error: () => {
        this._loading$.next(false);
      },
    });
  }

  onInvite(): void {
    this.inviteRequested.emit();
  }

  onLeaveGroup(): void {
    if (!this.conversationId) {
      return;
    }
    const confirmed = confirm('Leave this group? You can rejoin later from Browse Groups.');
    if (!confirmed) {
      return;
    }
    this._leaving$.next(true);
    this.conversationService.leaveGroup(this.conversationId).subscribe({
      next: () => {
        this._leaving$.next(false);
        this.leftGroup.emit();
      },
      error: () => {
        this._leaving$.next(false);
      },
    });
  }

  getMemberIds(): string[] {
    return this._members$.value.map(m => m.userId);
  }
}
