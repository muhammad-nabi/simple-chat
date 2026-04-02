import { Component, OnInit, OnDestroy, ElementRef, ViewChild, ViewChildren, QueryList, inject, output } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';
import { TeamMember } from '../../models/user.model';
import { AvatarComponent } from '../../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-new-chat-dialog',
  standalone: true,
  imports: [AsyncPipe, FormsModule, AvatarComponent],
  templateUrl: './new-chat-dialog.component.html',
  styleUrl: './new-chat-dialog.component.scss',
})
export class NewChatDialogComponent implements OnInit, OnDestroy {
  private readonly userService = inject(UserService);

  @ViewChild('filterInput') filterInput!: ElementRef<HTMLInputElement>;
  @ViewChildren('memberItem') memberItems!: QueryList<ElementRef<HTMLElement>>;

  readonly loading$ = this.userService.loading$;
  readonly error$ = this.userService.error$;

  readonly userSelected = output<TeamMember>();
  readonly closed = output<void>();

  filterText = '';
  filteredMembers: TeamMember[] = [];
  activeIndex = -1;

  private allMembers: TeamMember[] = [];
  private subscriptions: Subscription[] = [];

  ngOnInit(): void {
    this.userService.loadTeamMembers();
    const membersSub = this.userService.teamMembers$.subscribe((members: TeamMember[]) => {
      this.allMembers = members;
      this.applyFilter();
    });
    this.subscriptions.push(membersSub);

    setTimeout(() => this.filterInput?.nativeElement.focus(), 0);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
  }

  onFilterChange(): void {
    this.activeIndex = -1;
    this.applyFilter();
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (this.filteredMembers.length > 0) {
        this.activeIndex = Math.min(this.activeIndex + 1, this.filteredMembers.length - 1);
        this.scrollActiveIntoView();
      }
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.activeIndex > 0) {
        this.activeIndex--;
        this.scrollActiveIntoView();
      }
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      if (this.activeIndex >= 0 && this.activeIndex < this.filteredMembers.length) {
        this.selectMember(this.filteredMembers[this.activeIndex]);
      }
      return;
    }
  }

  selectMember(member: TeamMember): void {
    this.userSelected.emit(member);
  }

  close(): void {
    this.closed.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('dialog-backdrop')) {
      this.close();
    }
  }

  private scrollActiveIntoView(): void {
    const items = this.memberItems?.toArray();
    if (items && this.activeIndex >= 0 && this.activeIndex < items.length) {
      items[this.activeIndex].nativeElement.scrollIntoView?.({ block: 'nearest' });
    }
  }

  private applyFilter(): void {
    const term = this.filterText.toLowerCase().trim();
    if (!term) {
      this.filteredMembers = [...this.allMembers];
    } else {
      this.filteredMembers = this.allMembers.filter(
        (m: TeamMember) => (m.displayName ?? '').toLowerCase().includes(term)
      );
    }
  }
}
