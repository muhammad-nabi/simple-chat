import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, of } from 'rxjs';
import { InviteToGroupComponent } from './invite-to-group.component';
import { ConversationService } from '../../services/conversation.service';
import { UserService } from '../../services/user.service';
import { TeamMember } from '../../models/user.model';

describe('InviteToGroupComponent', () => {
  let component: InviteToGroupComponent;
  let fixture: ComponentFixture<InviteToGroupComponent>;
  let mockConversationService: {
    inviteToGroup: jest.Mock;
  };
  let mockTeamMembers$: BehaviorSubject<TeamMember[]>;
  let mockLoading$: BehaviorSubject<boolean>;

  const allMembers: TeamMember[] = [
    { userId: 'user-1', displayName: 'Alice' },
    { userId: 'user-2', displayName: 'Bob' },
    { userId: 'user-3', displayName: 'Charlie' },
    { userId: 'user-4', displayName: 'Diana' },
  ];

  beforeEach(async () => {
    mockConversationService = {
      inviteToGroup: jest.fn().mockReturnValue(of(void 0)),
    };
    mockTeamMembers$ = new BehaviorSubject<TeamMember[]>(allMembers);
    mockLoading$ = new BehaviorSubject<boolean>(false);

    await TestBed.configureTestingModule({
      imports: [InviteToGroupComponent],
      providers: [
        { provide: ConversationService, useValue: mockConversationService },
        {
          provide: UserService,
          useValue: {
            loadTeamMembers: jest.fn(),
            teamMembers$: mockTeamMembers$.asObservable(),
            loading$: mockLoading$.asObservable(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InviteToGroupComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should filter out existing members', () => {
    component.existingMemberIds = ['user-1', 'user-2'];
    component.ngOnInit();
    fixture.detectChanges();

    const filtered = component.filteredUsers;
    expect(filtered.length).toBe(2);
    expect(filtered.map(u => u.userId)).toEqual(['user-3', 'user-4']);
  });

  it('should toggle user selection', () => {
    component.existingMemberIds = [];
    component.ngOnInit();

    component.toggleUser('user-3');
    expect(component.isSelected('user-3')).toBe(true);

    component.toggleUser('user-3');
    expect(component.isSelected('user-3')).toBe(false);
  });

  it('should support multi-select', () => {
    component.existingMemberIds = [];
    component.ngOnInit();

    component.toggleUser('user-3');
    component.toggleUser('user-4');

    expect(component.selectedUserIds).toEqual(['user-3', 'user-4']);
  });

  it('should call inviteToGroup on invite', () => {
    component.conversationId = 1;
    component.existingMemberIds = [];
    component.ngOnInit();

    component.toggleUser('user-3');
    component.onInvite();

    expect(mockConversationService.inviteToGroup).toHaveBeenCalledWith(1, ['user-3']);
  });

  it('should emit invited event on success', () => {
    const spy = jest.spyOn(component.invited, 'emit');
    component.conversationId = 1;
    component.existingMemberIds = [];
    component.ngOnInit();

    component.toggleUser('user-3');
    component.onInvite();

    expect(spy).toHaveBeenCalledWith(['user-3']);
  });

  it('should emit closed on cancel', () => {
    const spy = jest.spyOn(component.closed, 'emit');

    component.onCancel();

    expect(spy).toHaveBeenCalled();
  });

  it('should not call inviteToGroup when no selection', () => {
    component.conversationId = 1;
    component.existingMemberIds = [];
    component.ngOnInit();

    component.onInvite();

    expect(mockConversationService.inviteToGroup).not.toHaveBeenCalled();
  });

  it('should filter users by search text', () => {
    component.existingMemberIds = [];
    component.ngOnInit();

    component.filterText = 'ali';
    const filtered = component.filteredUsers;

    expect(filtered.length).toBe(1);
    expect(filtered[0].displayName).toBe('Alice');
  });
});
