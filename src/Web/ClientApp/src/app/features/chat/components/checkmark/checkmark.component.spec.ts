import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CheckmarkComponent } from './checkmark.component';

describe('CheckmarkComponent', () => {
  let component: CheckmarkComponent;
  let fixture: ComponentFixture<CheckmarkComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CheckmarkComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CheckmarkComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    component.state = 'sending';
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should render checkmark character', () => {
    component.state = 'sending';
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement.querySelector('.checkmark');
    expect(el.textContent?.trim()).toBe('\u2713');
  });

  it('should apply sending class when state is sending', () => {
    component.state = 'sending';
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement.querySelector('.checkmark');
    expect(el.classList.contains('sending')).toBe(true);
    expect(el.classList.contains('sent')).toBe(false);
  });

  it('should apply sent class when state is sent', () => {
    component.state = 'sent';
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement.querySelector('.checkmark');
    expect(el.classList.contains('sent')).toBe(true);
    expect(el.classList.contains('sending')).toBe(false);
  });

  it('should apply celebrate class when animate is true and state is sent', () => {
    component.state = 'sent';
    component.animate = true;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement.querySelector('.checkmark');
    expect(el.classList.contains('celebrate')).toBe(true);
  });

  it('should not apply celebrate class when animate is false', () => {
    component.state = 'sent';
    component.animate = false;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement.querySelector('.checkmark');
    expect(el.classList.contains('celebrate')).toBe(false);
  });

  it('should not apply celebrate class when state is sending', () => {
    component.state = 'sending';
    component.animate = true;
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement.querySelector('.checkmark');
    expect(el.classList.contains('celebrate')).toBe(false);
  });

  it('should have aria-hidden on checkmark element', () => {
    component.state = 'sending';
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement.querySelector('.checkmark');
    expect(el.getAttribute('aria-hidden')).toBe('true');
  });

  it('should default animate to false', () => {
    expect(component.animate).toBe(false);
  });
});
