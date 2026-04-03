import { Component, OnInit, OnDestroy, ElementRef, ViewChild, ViewChildren, QueryList, inject, output } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';
import { TeamMember } from '../../models/user.model';
import { AvatarComponent } from '../../../../shared/components/avatar/avatar.component';

export interface GroupCreationResult {
  participantIds: string[];
  groupName: string;
}

@Component({
  selector: 'app-new-chat-dialog',
  standalone: true,
  imports: [AsyncPipe, FormsModule, ReactiveFormsModule, AvatarComponent],
  templateUrl: './new-chat-dialog.component.html',
  styleUrl: './new-chat-dialog.component.scss',
})
export class NewChatDialogComponent implements OnInit, OnDestroy {
  private readonly userService = inject(UserService);
  private readonly fb = inject(FormBuilder);

  @ViewChild('filterInput') filterInput!: ElementRef<HTMLInputElement>;
  @ViewChildren('memberItem') memberItems!: QueryList<ElementRef<HTMLElement>>;

  readonly loading$ = this.userService.loading$;
  readonly error$ = this.userService.error$;

  readonly userSelected = output<TeamMember>();
  readonly groupCreated = output<GroupCreationResult>();
  readonly closed = output<void>();

  filterText = '';
  filteredMembers: TeamMember[] = [];
  activeIndex = -1;
  selectedMembers: TeamMember[] = [];

  groupForm: FormGroup = this.fb.group({
    groupName: ['', [Validators.required, Validators.maxLength(100)]],
  });

  private allMembers: TeamMember[] = [];
  private subscriptions: Subscription[] = [];

  get isGroupMode(): boolean {
    return this.selectedMembers.length > 1;
  }

  get isGroupNameValid(): boolean {
    const value = this.groupForm.controls['groupName'].value;
    return this.groupForm.controls['groupName'].valid && value != null && value.trim().length > 0;
  }

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

    if (event.key === 'Backspace' && this.filterText === '' && this.selectedMembers.length > 0) {
      this.removeSelectedMember(this.selectedMembers[this.selectedMembers.length - 1]);
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
        this.toggleMember(this.filteredMembers[this.activeIndex]);
      }
      return;
    }
  }

  toggleMember(member: TeamMember): void {
    const index = this.selectedMembers.findIndex(m => m.userId === member.userId);
    if (index >= 0) {
      this.selectedMembers = [...this.selectedMembers.slice(0, index), ...this.selectedMembers.slice(index + 1)];
    } else {
      this.selectedMembers = [...this.selectedMembers, member];
    }

    // If exactly one member selected and not in group mode, auto-select for DM
    if (this.selectedMembers.length === 1 && !this.isGroupMode) {
      // Don't auto-emit — wait for user to click again or select more
    }
  }

  selectMember(member: TeamMember): void {
    this.toggleMember(member);
  }

  isMemberSelected(member: TeamMember): boolean {
    return this.selectedMembers.some(m => m.userId === member.userId);
  }

  removeSelectedMember(member: TeamMember): void {
    this.selectedMembers = this.selectedMembers.filter(m => m.userId !== member.userId);
  }

  onGroupNameKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      event.stopPropagation();
      if (this.isGroupNameValid) {
        this.startConversation();
      }
    }
  }

  startConversation(): void {
    if (this.selectedMembers.length === 1) {
      this.userSelected.emit(this.selectedMembers[0]);
    } else if (this.isGroupMode && this.isGroupNameValid) {
      this.groupCreated.emit({
        participantIds: this.selectedMembers.map(m => m.userId),
        groupName: this.groupForm.controls['groupName'].value.trim(),
      });
    }
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
