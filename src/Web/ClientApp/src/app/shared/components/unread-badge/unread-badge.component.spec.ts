import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UnreadBadgeComponent } from './unread-badge.component';

describe('UnreadBadgeComponent', () => {
  let fixture: ComponentFixture<UnreadBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UnreadBadgeComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(UnreadBadgeComponent);
    fixture.componentRef.setInput('count', 5);
    fixture.detectChanges();
  });

  it('should display count', () => {
    const badge = fixture.nativeElement.querySelector('.unread-badge');
    expect(badge).toBeTruthy();
    expect(badge.textContent.trim()).toBe('5');
  });

  it('should display 99+ for counts over 99', () => {
    fixture.componentRef.setInput('count', 150);
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.unread-badge');
    expect(badge.textContent.trim()).toBe('99+');
  });

  it('should be hidden when count is 0', () => {
    fixture.componentRef.setInput('count', 0);
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.unread-badge');
    expect(badge).toBeNull();
  });

  it('should have aria-label with count', () => {
    const badge = fixture.nativeElement.querySelector('.unread-badge');
    expect(badge.getAttribute('aria-label')).toBe('5 unread messages');
  });

  it('should have singular aria-label for count of 1', () => {
    fixture.componentRef.setInput('count', 1);
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.unread-badge');
    expect(badge.getAttribute('aria-label')).toBe('1 unread message');
  });
});
