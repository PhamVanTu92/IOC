import { renderHook, act, waitFor } from '@testing-library/react';
import { useSignalR, invokeHub } from '../../../../src/frontend/src/shared/hooks/useSignalR';

// ─────────────────────────────────────────────────────────────────────────────
// Mock @microsoft/signalr
// ─────────────────────────────────────────────────────────────────────────────

type Handler = (...args: unknown[]) => void;

class FakeHubConnection {
  private _handlers: Map<string, Handler[]> = new Map();
  private _reconnectingCb?: () => void;
  private _reconnectedCb?: () => void;
  private _closeCb?: () => void;
  state = 'Disconnected';

  async start() {
    this.state = 'Connected';
  }
  async stop() {
    this.state = 'Disconnected';
    this._closeCb?.();
  }

  onreconnecting(cb: () => void) { this._reconnectingCb = cb; }
  onreconnected(cb: () => void) { this._reconnectedCb = cb; }
  onclose(cb: () => void) { this._closeCb = cb; }

  on(method: string, handler: Handler) {
    if (!this._handlers.has(method)) this._handlers.set(method, []);
    this._handlers.get(method)!.push(handler);
  }
  off(method: string, handler: Handler) {
    const list = this._handlers.get(method) ?? [];
    this._handlers.set(method, list.filter(h => h !== handler));
  }

  async invoke(method: string, ...args: unknown[]) {
    return { method, args };
  }

  // Test helpers
  simulateReconnecting() { this._reconnectingCb?.(); }
  simulateReconnected() {
    this.state = 'Connected';
    this._reconnectedCb?.();
  }
  emit(method: string, ...args: unknown[]) {
    this._handlers.get(method)?.forEach(h => h(...args));
  }
  handlerCount(method: string) {
    return this._handlers.get(method)?.length ?? 0;
  }
}

let fakeConn: FakeHubConnection;

jest.mock('@microsoft/signalr', () => {
  return {
    HubConnectionBuilder: jest.fn().mockImplementation(() => ({
      withUrl: jest.fn().mockReturnThis(),
      withAutomaticReconnect: jest.fn().mockReturnThis(),
      configureLogging: jest.fn().mockReturnThis(),
      build: jest.fn().mockImplementation(() => {
        fakeConn = new FakeHubConnection();
        return fakeConn;
      }),
    })),
    HubConnectionState: { Connected: 'Connected', Disconnected: 'Disconnected' },
    LogLevel: { Information: 1, Warning: 2 },
  };
});

// ─────────────────────────────────────────────────────────────────────────────
// Tests
// ─────────────────────────────────────────────────────────────────────────────

describe('useSignalR', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('starts as "connecting" then transitions to "connected"', async () => {
    const { result } = renderHook(() => useSignalR());

    expect(result.current.status).toBe('connecting');

    await waitFor(() => {
      expect(result.current.status).toBe('connected');
    });
  });

  it('calls onConnected callback after connection starts', async () => {
    const onConnected = jest.fn();
    renderHook(() => useSignalR({ onConnected }));

    await waitFor(() => {
      expect(onConnected).toHaveBeenCalledTimes(1);
    });
  });

  it('reflects "reconnecting" state when hub fires onreconnecting', async () => {
    const { result } = renderHook(() => useSignalR());
    await waitFor(() => expect(result.current.status).toBe('connected'));

    act(() => { fakeConn.simulateReconnecting(); });
    expect(result.current.status).toBe('reconnecting');
  });

  it('returns to "connected" after successful reconnect', async () => {
    const { result } = renderHook(() => useSignalR());
    await waitFor(() => expect(result.current.status).toBe('connected'));

    act(() => { fakeConn.simulateReconnecting(); });
    act(() => { fakeConn.simulateReconnected(); });

    expect(result.current.status).toBe('connected');
  });

  it('does not start connection when skip=true', async () => {
    const { result } = renderHook(() => useSignalR({ skip: true }));
    // After a tick nothing should have changed
    await act(async () => {});
    expect(result.current.status).toBe('disconnected');
  });

  it('registers event handlers via on() and fires them on emit', async () => {
    const handler = jest.fn();
    const { result } = renderHook(() => useSignalR());
    await waitFor(() => expect(result.current.status).toBe('connected'));

    act(() => { result.current.on('TestEvent', handler); });
    act(() => { fakeConn.emit('TestEvent', { value: 42 }); });

    expect(handler).toHaveBeenCalledWith({ value: 42 });
  });

  it('removes handler via off()', async () => {
    const handler = jest.fn();
    const { result } = renderHook(() => useSignalR());
    await waitFor(() => expect(result.current.status).toBe('connected'));

    const handlerRef = handler as (...args: unknown[]) => void;
    act(() => { result.current.on('TestEvent', handler); });
    act(() => { result.current.off('TestEvent', handlerRef); });
    act(() => { fakeConn.emit('TestEvent', {}); });

    expect(handler).not.toHaveBeenCalled();
  });

  it('stops connection on unmount', async () => {
    const stopSpy = jest.spyOn(FakeHubConnection.prototype, 'stop');
    const { unmount } = renderHook(() => useSignalR());
    await waitFor(() => expect(fakeConn.state).toBe('Connected'));

    unmount();
    expect(stopSpy).toHaveBeenCalled();
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// invokeHub helper
// ─────────────────────────────────────────────────────────────────────────────

describe('invokeHub', () => {
  it('calls invoke when connection is Connected', () => {
    const conn = new FakeHubConnection();
    conn.state = 'Connected';
    const spy = jest.spyOn(conn, 'invoke');
    invokeHub(conn as unknown as Parameters<typeof invokeHub>[0], 'TestMethod', 'arg1');
    expect(spy).toHaveBeenCalledWith('TestMethod', 'arg1');
  });

  it('does nothing when connection is null', () => {
    // Should not throw
    expect(() => invokeHub(null, 'TestMethod')).not.toThrow();
  });

  it('does nothing when connection state is Disconnected', () => {
    const conn = new FakeHubConnection();
    conn.state = 'Disconnected';
    const spy = jest.spyOn(conn, 'invoke');
    invokeHub(conn as unknown as Parameters<typeof invokeHub>[0], 'TestMethod');
    expect(spy).not.toHaveBeenCalled();
  });
});
