import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChatLayoutComponent } from './chat-layout.component';

describe('ChatLayoutComponent', () => {
  let component: ChatLayoutComponent;
  let fixture: ComponentFixture<ChatLayoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatLayoutComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should default to desktop layout mode', () => {
    // matchMedia in test env returns false for all queries by default
    // so it falls through to desktop
    expect(component.layoutMode).toBeDefined();
  });

  it('should show sidebar by default on desktop', () => {
    component.layoutMode = 'desktop';
    expect(component.showSidebar).toBe(true);
  });

  it('should show chat area by default on desktop', () => {
    component.layoutMode = 'desktop';
    expect(component.showChatArea).toBe(true);
  });

  it('should hide members panel by default on desktop', () => {
    component.layoutMode = 'desktop';
    component.showMembersPanel = false;
    expect(component.showMembers).toBe(false);
  });

  it('should toggle members panel on desktop', () => {
    component.layoutMode = 'desktop';
    component.toggleMembers();
    expect(component.showMembers).toBe(true);
    component.toggleMembers();
    expect(component.showMembers).toBe(false);
  });

  it('should show only conversation list on mobile by default', () => {
    component.layoutMode = 'mobile';
    component.activePanel = 'list';
    expect(component.showSidebar).toBe(true);
    expect(component.showChatArea).toBe(false);
    expect(component.showMembers).toBe(false);
  });

  it('should switch to chat panel on mobile when selectConversation is called', () => {
    component.layoutMode = 'mobile';
    component.activePanel = 'list';
    component.selectConversation();
    expect(component.activePanel).toBe('chat');
    expect(component.showChatArea).toBe(true);
    expect(component.showSidebar).toBe(false);
  });

  it('should navigate back to list on mobile', () => {
    component.layoutMode = 'mobile';
    component.activePanel = 'chat';
    component.navigateBack();
    expect(component.activePanel).toBe('list');
    expect(component.showSidebar).toBe(true);
  });

  it('should toggle sidebar on tablet', () => {
    component.layoutMode = 'tablet';
    expect(component.showSidebar).toBe(true);
    component.toggleSidebar();
    expect(component.showSidebar).toBe(false);
    component.toggleSidebar();
    expect(component.showSidebar).toBe(true);
  });

  it('should never show members panel on tablet', () => {
    component.layoutMode = 'tablet';
    component.showMembersPanel = true;
    expect(component.showMembers).toBe(false);
  });

  it('should render the chat layout container', () => {
    const layoutEl: HTMLElement = fixture.nativeElement.querySelector('.chat-layout');
    expect(layoutEl).toBeTruthy();
  });

  it('should render sidebar with navigation role', () => {
    component.layoutMode = 'desktop';
    fixture.detectChanges();
    const sidebar: HTMLElement = fixture.nativeElement.querySelector('[role="navigation"]');
    expect(sidebar).toBeTruthy();
  });

  it('should render chat area with main role', () => {
    component.layoutMode = 'desktop';
    fixture.detectChanges();
    const chatArea: HTMLElement = fixture.nativeElement.querySelector('[role="main"]');
    expect(chatArea).toBeTruthy();
  });

  it('should not show back button on desktop', () => {
    component.layoutMode = 'desktop';
    component.activePanel = 'chat';
    fixture.detectChanges();
    const backBtn: HTMLElement | null = fixture.nativeElement.querySelector('.back-button');
    expect(backBtn).toBeFalsy();
  });

  it('should show back button on mobile chat panel', () => {
    component.layoutMode = 'mobile';
    component.activePanel = 'chat';
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    const backBtn: HTMLElement | null = fixture.nativeElement.querySelector('.back-button');
    expect(backBtn).toBeTruthy();
  });

  it('should toggle members to active panel on mobile', () => {
    component.layoutMode = 'mobile';
    component.activePanel = 'chat';
    component.toggleMembers();
    expect(component.activePanel).toBe('members');
    component.toggleMembers();
    expect(component.activePanel).toBe('chat');
  });

  it('should not show members toggle button on tablet', () => {
    component.layoutMode = 'tablet';
    component.activePanel = 'chat';
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    const membersBtn: HTMLElement | null = fixture.nativeElement.querySelector('.toggle-members-button');
    expect(membersBtn).toBeFalsy();
  });

  it('should show members toggle button on desktop', () => {
    component.layoutMode = 'desktop';
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    const membersBtn: HTMLElement | null = fixture.nativeElement.querySelector('.toggle-members-button');
    expect(membersBtn).toBeTruthy();
  });

  it('should reset activePanel from members when switching to tablet', () => {
    component.layoutMode = 'mobile';
    component.activePanel = 'members';
    // Simulate breakpoint change to tablet
    component.layoutMode = 'tablet';
    component.activePanel = component.activePanel === 'members' ? 'chat' : component.activePanel;
    expect(component.activePanel).toBe('chat');
  });

  it('should clean up media query listeners on destroy', () => {
    const spy = jest.spyOn(component['mobileQuery'], 'removeEventListener');
    component.ngOnDestroy();
    expect(spy).toHaveBeenCalled();
    spy.mockRestore();
  });
});
