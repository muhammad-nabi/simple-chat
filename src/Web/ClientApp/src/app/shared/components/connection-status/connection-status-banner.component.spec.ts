import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { BehaviorSubject } from 'rxjs';
import { ConnectionStatusBannerComponent } from './connection-status-banner.component';
import { ConnectionState } from '../../../core/signalr/signalr.service';

describe('ConnectionStatusBannerComponent', () => {
  let fixture: ComponentFixture<ConnectionStatusBannerComponent>;
  let component: ConnectionStatusBannerComponent;
  let connectionState$: BehaviorSubject<ConnectionState>;

  beforeEach(async () => {
    connectionState$ = new BehaviorSubject<ConnectionState>('Connected');

    const mockSignalRService = {
      connectionState: connectionState$.asObservable(),
    };

    await TestBed.configureTestingModule({
      imports: [ConnectionStatusBannerComponent],
      providers: [
        { provide: 'SignalRService', useValue: mockSignalRService },
      ],
    })
      .overrideComponent(ConnectionStatusBannerComponent, {
        set: {
          providers: [],
        },
      })
      .compileComponents();

    // Override the inject() call by patching the prototype before creating the component
    TestBed.overrideProvider(
      // Use the actual SignalRService token
      (await import('../../../core/signalr/signalr.service')).SignalRService,
      { useValue: mockSignalRService },
    );

    fixture = TestBed.createComponent(ConnectionStatusBannerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should hide banner when connected', fakeAsync(() => {
    connectionState$.next('Connected');
    tick(0);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner');
    expect(banner).toBeNull();
  }));

  it('should not show banner during brief reconnecting (<3s)', fakeAsync(() => {
    connectionState$.next('Reconnecting');
    tick(2000); // Only 2 seconds elapsed
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner');
    expect(banner).toBeNull();

    // Reconnect before 3s
    connectionState$.next('Connected');
    tick(1500);
    fixture.detectChanges();

    const bannerAfter = fixture.nativeElement.querySelector('.connection-banner');
    expect(bannerAfter).toBeNull();
  }));

  it('should show amber banner after 3s reconnecting', fakeAsync(() => {
    connectionState$.next('Reconnecting');
    tick(3000);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner.reconnecting');
    expect(banner).toBeTruthy();
    expect(banner.textContent.trim()).toBe('Reconnecting...');
  }));

  it('should show red banner immediately on disconnected', fakeAsync(() => {
    connectionState$.next('Disconnected');
    tick(0);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner.disconnected');
    expect(banner).toBeTruthy();
    expect(banner.textContent.trim()).toBe('Connection lost. Check your internet connection.');
  }));

  it('should hide banner when reconnection succeeds', fakeAsync(() => {
    connectionState$.next('Reconnecting');
    tick(3000);
    fixture.detectChanges();

    let banner = fixture.nativeElement.querySelector('.connection-banner.reconnecting');
    expect(banner).toBeTruthy();

    connectionState$.next('Connected');
    tick(0);
    fixture.detectChanges();

    banner = fixture.nativeElement.querySelector('.connection-banner');
    expect(banner).toBeNull();
  }));

  it('should have role="status" on reconnecting banner', fakeAsync(() => {
    connectionState$.next('Reconnecting');
    tick(3000);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner');
    expect(banner.getAttribute('role')).toBe('status');
  }));

  it('should have aria-live="assertive" on reconnecting banner', fakeAsync(() => {
    connectionState$.next('Reconnecting');
    tick(3000);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner');
    expect(banner.getAttribute('aria-live')).toBe('assertive');
  }));

  it('should have role="status" on disconnected banner', fakeAsync(() => {
    connectionState$.next('Disconnected');
    tick(0);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner');
    expect(banner.getAttribute('role')).toBe('status');
  }));

  it('should have aria-live="assertive" on disconnected banner', fakeAsync(() => {
    connectionState$.next('Disconnected');
    tick(0);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner');
    expect(banner.getAttribute('aria-live')).toBe('assertive');
  }));

  it('should transition from disconnected to hidden on connected', fakeAsync(() => {
    connectionState$.next('Disconnected');
    tick(0);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.connection-banner.disconnected')).toBeTruthy();

    connectionState$.next('Connected');
    tick(0);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.connection-banner')).toBeNull();
  }));

  it('should show disconnected immediately even during reconnecting debounce', fakeAsync(() => {
    connectionState$.next('Reconnecting');
    tick(1000); // Only 1 second into debounce
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.connection-banner')).toBeNull();

    // Transition to disconnected while still in debounce
    connectionState$.next('Disconnected');
    tick(0);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.connection-banner.disconnected');
    expect(banner).toBeTruthy();
  }));
});
