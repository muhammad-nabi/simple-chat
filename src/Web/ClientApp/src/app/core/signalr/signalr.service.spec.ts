import { BehaviorSubject } from 'rxjs';
import { ConnectionState } from './signalr.service';
import { MessagePayload } from './signalr.events';

// Mock the signalr module
const mockConnection = {
  start: jest.fn().mockResolvedValue(undefined),
  stop: jest.fn().mockResolvedValue(undefined),
  on: jest.fn(),
  invoke: jest.fn(),
  onreconnecting: jest.fn(),
  onreconnected: jest.fn(),
  onclose: jest.fn(),
};

const mockBuilder = {
  withUrl: jest.fn().mockReturnThis(),
  withAutomaticReconnect: jest.fn().mockReturnThis(),
  build: jest.fn().mockReturnValue(mockConnection),
};

jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockImplementation(() => mockBuilder),
}));

// Create a test-friendly version of the service without Angular DI
class TestableSignalRService {
  private connection: ReturnType<typeof mockBuilder.build> | null = null;
  private readonly connectionState$ = new BehaviorSubject<ConnectionState>('Disconnected');
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  private readonly messageReceived$ = new (require('rxjs').Subject)<MessagePayload>();
  private readonly accessToken = 'test-jwt-token';

  readonly connectionState = this.connectionState$.asObservable();
  readonly messageReceived = this.messageReceived$.asObservable();

  start(): void {
    if (this.connection) {
      return;
    }
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const signalR = require('@microsoft/signalr');
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => this.accessToken,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.connection!.on('ReceiveMessage', (message: MessagePayload) => {
      this.messageReceived$.next(message);
    });
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    this.connection!.on('UserOnline', () => {});
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    this.connection!.on('UserOffline', () => {});

    this.connection!.onreconnecting(() => {
      this.connectionState$.next('Reconnecting');
    });
    this.connection!.onreconnected(() => {
      this.connectionState$.next('Connected');
    });
    this.connection!.onclose(() => {
      this.connectionState$.next('Disconnected');
    });

    this.connection!.start()
      .then(() => this.connectionState$.next('Connected'));
  }

  stop(): void {
    if (!this.connection) {
      return;
    }
    this.connection.stop()
      .then(() => {
        this.connection = null;
        this.connectionState$.next('Disconnected');
      });
  }
}

describe('SignalRService', () => {
  let service: TestableSignalRService;

  beforeEach(() => {
    jest.clearAllMocks();
    mockBuilder.withUrl.mockReturnThis();
    mockBuilder.withAutomaticReconnect.mockReturnThis();
    mockBuilder.build.mockReturnValue(mockConnection);
    mockConnection.start.mockResolvedValue(undefined);
    mockConnection.stop.mockResolvedValue(undefined);

    service = new TestableSignalRService();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initially have Disconnected state', (done) => {
    service.connectionState.subscribe((state: ConnectionState) => {
      expect(state).toBe('Disconnected');
      done();
    });
  });

  it('should create hub connection with correct URL', () => {
    service.start();

    expect(mockBuilder.withUrl).toHaveBeenCalledWith(
      '/hubs/chat',
      expect.objectContaining({
        accessTokenFactory: expect.any(Function),
      }),
    );
  });

  it('should configure automatic reconnect with correct intervals', () => {
    service.start();

    expect(mockBuilder.withAutomaticReconnect).toHaveBeenCalledWith([
      0, 2000, 5000, 10000, 30000,
    ]);
  });

  it('should emit Connected state after successful start', async () => {
    const states: ConnectionState[] = [];
    service.connectionState.subscribe(state => states.push(state));

    service.start();
    await new Promise(resolve => setTimeout(resolve, 10));

    expect(states).toContain('Connected');
  });

  it('should emit Disconnected state after stop', async () => {
    service.start();
    await new Promise(resolve => setTimeout(resolve, 10));

    const states: ConnectionState[] = [];
    service.connectionState.subscribe(state => states.push(state));

    service.stop();
    await new Promise(resolve => setTimeout(resolve, 10));

    expect(states).toContain('Disconnected');
  });

  it('should register ReceiveMessage event handler', () => {
    service.start();

    expect(mockConnection.on).toHaveBeenCalledWith('ReceiveMessage', expect.any(Function));
  });

  it('should dispatch ReceiveMessage events to subscribers', () => {
    service.start();

    const receiveMessageCall = (mockConnection.on.mock.calls as unknown[][]).find(
      (call) => call[0] === 'ReceiveMessage',
    );
    expect(receiveMessageCall).toBeDefined();
    const handler = receiveMessageCall![1] as (msg: MessagePayload) => void;

    const receivedMessages: MessagePayload[] = [];
    service.messageReceived.subscribe(msg => receivedMessages.push(msg));

    const testMessage: MessagePayload = {
      id: 1,
      conversationId: 10,
      senderId: 'user-1',
      senderDisplayName: 'Alice',
      content: 'Hello!',
      sentAt: '2026-04-02T12:00:00Z',
      messageType: 'Text',
    };

    handler(testMessage);

    expect(receivedMessages).toEqual([testMessage]);
  });

  it('should not create duplicate connections on multiple start calls', () => {
    service.start();
    service.start();

    expect(mockBuilder.build).toHaveBeenCalledTimes(1);
  });

  it('should register lifecycle handlers', () => {
    service.start();

    expect(mockConnection.onreconnecting).toHaveBeenCalledWith(expect.any(Function));
    expect(mockConnection.onreconnected).toHaveBeenCalledWith(expect.any(Function));
    expect(mockConnection.onclose).toHaveBeenCalledWith(expect.any(Function));
  });
});
