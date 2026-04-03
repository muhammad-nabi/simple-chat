import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { GroupMembersComponent } from './group-members.component';
import { ConversationService, GroupMemberDto } from '../../services/conversation.service';

describe('GroupMembersComponent', () => {
  let component: GroupMembersComponent;
  let fixture: ComponentFixture<GroupMembersComponent>;
  let mockConversationService: {
    getGroupMembers: jest.Mock;
    leaveGroup: jest.Mock;
  };

  const mockMembers: GroupMemberDto[] = [
    { userId: 'user-1', displayName: 'Alice', joinedAt: '2026-01-01T00:00:00Z' },
    { userId: 'user-2', displayName: 'Bob', joinedAt: '2026-01-02T00:00:00Z' },
  ];

  beforeEach(async () => {
    mockConversationService = {
      getGroupMembers: jest.fn().mockReturnValue(of(mockMembers)),
      leaveGroup: jest.fn().mockReturnValue(of(void 0)),
    };

    await TestBed.configureTestingModule({
      imports: [GroupMembersComponent],
      providers: [
        { provide: ConversationService, useValue: mockConversationService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupMembersComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load members when conversationId changes', () => {
    component.conversationId = 1;
    component.ngOnChanges({
      conversationId: {
        currentValue: 1,
        previousValue: null,
        firstChange: true,
        isFirstChange: () => true,
      },
    });

    expect(mockConversationService.getGroupMembers).toHaveBeenCalledWith(1);
  });

  it('should render member list', () => {
    component.conversationId = 1;
    component.ngOnChanges({
      conversationId: {
        currentValue: 1,
        previousValue: null,
        firstChange: true,
        isFirstChange: () => true,
      },
    });
    fixture.detectChanges();

    const memberItems = fixture.nativeElement.querySelectorAll('.member-item');
    expect(memberItems.length).toBe(2);
  });

  it('should emit inviteRequested on invite button click', () => {
    const spy = jest.spyOn(component.inviteRequested, 'emit');
    component.conversationId = 1;
    component.ngOnChanges({
      conversationId: {
        currentValue: 1,
        previousValue: null,
        firstChange: true,
        isFirstChange: () => true,
      },
    });
    fixture.detectChanges();

    const inviteBtn = fixture.nativeElement.querySelector('.invite-btn');
    inviteBtn.click();

    expect(spy).toHaveBeenCalled();
  });

  it('should call leaveGroup service on confirmed leave', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);
    component.conversationId = 1;
    component.ngOnChanges({
      conversationId: {
        currentValue: 1,
        previousValue: null,
        firstChange: true,
        isFirstChange: () => true,
      },
    });
    fixture.detectChanges();

    component.onLeaveGroup();

    expect(mockConversationService.leaveGroup).toHaveBeenCalledWith(1);
  });

  it('should not call leaveGroup when confirm is cancelled', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(false);
    component.conversationId = 1;

    component.onLeaveGroup();

    expect(mockConversationService.leaveGroup).not.toHaveBeenCalled();
  });

  it('should emit leftGroup after successful leave', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);
    const spy = jest.spyOn(component.leftGroup, 'emit');
    component.conversationId = 1;

    component.onLeaveGroup();

    expect(spy).toHaveBeenCalled();
  });

  it('should return member IDs', () => {
    component.conversationId = 1;
    component.ngOnChanges({
      conversationId: {
        currentValue: 1,
        previousValue: null,
        firstChange: true,
        isFirstChange: () => true,
      },
    });
    fixture.detectChanges();

    const ids = component.getMemberIds();
    expect(ids).toEqual(['user-1', 'user-2']);
  });
});
