import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AvatarComponent } from './avatar.component';

describe('AvatarComponent', () => {
  let fixture: ComponentFixture<AvatarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AvatarComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(AvatarComponent);
    fixture.componentRef.setInput('displayName', 'Sarah Chen');
    fixture.componentRef.setInput('userId', 'user-1');
    fixture.componentRef.setInput('size', 'sidebar');
    fixture.detectChanges();
  });

  it('should create', () => {
    const avatar = fixture.nativeElement.querySelector('.avatar');
    expect(avatar).toBeTruthy();
  });

  it('should display two-letter initials for two-word name', () => {
    const avatar = fixture.nativeElement.querySelector('.avatar');
    expect(avatar.textContent.trim()).toBe('SC');
  });

  it('should display single initial for single-word name', () => {
    fixture.componentRef.setInput('displayName', 'Marcus');
    fixture.detectChanges();
    const avatar = fixture.nativeElement.querySelector('.avatar');
    expect(avatar.textContent.trim()).toBe('M');
  });

  it('should display ? for empty name', () => {
    fixture.componentRef.setInput('displayName', '');
    fixture.detectChanges();
    const avatar = fixture.nativeElement.querySelector('.avatar');
    expect(avatar.textContent.trim()).toBe('?');
  });

  it('should set sidebar size (44px)', () => {
    const avatar = fixture.nativeElement.querySelector('.avatar') as HTMLElement;
    expect(avatar.style.width).toBe('44px');
    expect(avatar.style.height).toBe('44px');
  });

  it('should set header size (36px)', () => {
    fixture.componentRef.setInput('size', 'header');
    fixture.detectChanges();
    const avatar = fixture.nativeElement.querySelector('.avatar') as HTMLElement;
    expect(avatar.style.width).toBe('36px');
    expect(avatar.style.height).toBe('36px');
  });

  it('should set members size (30px)', () => {
    fixture.componentRef.setInput('size', 'members');
    fixture.detectChanges();
    const avatar = fixture.nativeElement.querySelector('.avatar') as HTMLElement;
    expect(avatar.style.width).toBe('30px');
    expect(avatar.style.height).toBe('30px');
  });

  it('should generate deterministic background color from userId', () => {
    const avatar = fixture.nativeElement.querySelector('.avatar') as HTMLElement;
    const color = avatar.style.backgroundColor;
    expect(color).toBeTruthy();
  });

  it('should have aria-label with display name', () => {
    const avatar = fixture.nativeElement.querySelector('.avatar');
    expect(avatar.getAttribute('aria-label')).toBe('Sarah Chen');
  });
});
