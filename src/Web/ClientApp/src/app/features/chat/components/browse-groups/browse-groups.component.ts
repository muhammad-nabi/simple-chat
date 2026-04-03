import { Component, inject, OnInit, output } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { BehaviorSubject, Observable } from 'rxjs';
import { ConversationService, BrowseGroupDto } from '../../services/conversation.service';
import { AvatarComponent } from '../../../../shared/components/avatar/avatar.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';

@Component({
  selector: 'app-browse-groups',
  standalone: true,
  imports: [AsyncPipe, AvatarComponent, RelativeTimePipe],
  templateUrl: './browse-groups.component.html',
  styleUrl: './browse-groups.component.scss',
})
export class BrowseGroupsComponent implements OnInit {
  private readonly conversationService = inject(ConversationService);

  private readonly _groups$ = new BehaviorSubject<BrowseGroupDto[]>([]);
  private readonly _loading$ = new BehaviorSubject<boolean>(false);
  private readonly _joiningId$ = new BehaviorSubject<number | null>(null);

  readonly groups$: Observable<BrowseGroupDto[]> = this._groups$.asObservable();
  readonly loading$: Observable<boolean> = this._loading$.asObservable();
  readonly joiningId$: Observable<number | null> = this._joiningId$.asObservable();

  readonly groupJoined = output<number>();
  readonly closed = output<void>();

  ngOnInit(): void {
    this.loadGroups();
  }

  loadGroups(): void {
    this._loading$.next(true);
    this.conversationService.loadBrowseGroups().subscribe({
      next: (groups: BrowseGroupDto[]) => {
        this._groups$.next(groups);
        this._loading$.next(false);
      },
      error: () => {
        this._groups$.next([]);
        this._loading$.next(false);
      },
    });
  }

  joinGroup(group: BrowseGroupDto): void {
    this._joiningId$.next(group.id);
    this.conversationService.joinGroup(group.id);

    const cleanup = (): void => {
      successSub.unsubscribe();
      errorSub.unsubscribe();
      this._joiningId$.next(null);
    };

    const successSub = this.conversationService.conversationCreated.subscribe((conversationId: number) => {
      cleanup();
      this.groupJoined.emit(conversationId);
    });

    const errorSub = this.conversationService.error.subscribe((error: string | null) => {
      if (error !== null) {
        cleanup();
      }
    });
  }

  goBack(): void {
    this.closed.emit();
  }
}
