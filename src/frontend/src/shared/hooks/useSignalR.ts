import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useCallback, useEffect, useRef, useState } from 'react';

// ─────────────────────────────────────────────────────────────────────────────
// useSignalR — manages a single HubConnection lifecycle
//
// Features:
//   • Automatic reconnection with exponential back-off (1s → 2s → 5s → 10s → 30s)
//   • Exposes connection state for UI indicator (connected/reconnecting/disconnected)
//   • Stable `on` / `off` helpers that survive re-renders without re-subscribing
//   • Hub URL defaults to VITE_SIGNALR_URL env var; falls back to gateway default
// ─────────────────────────────────────────────────────────────────────────────

export type SignalRStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

export interface UseSignalROptions {
  /** SignalR hub URL. Defaults to VITE_SIGNALR_URL + "/hubs/dashboard". */
  url?: string;
  /** Called once the connection is established (or re-established). */
  onConnected?: (connection: HubConnection) => void | Promise<void>;
  /** Skip connection entirely (useful when auth token not yet available). */
  skip?: boolean;
}

export interface UseSignalRReturn {
  connection: HubConnection | null;
  status: SignalRStatus;
  /** Subscribe to a hub server→client method. Stable across renders. */
  on: <TArgs extends unknown[]>(method: string, handler: (...args: TArgs) => void) => void;
  /** Remove a previously added handler. */
  off: (method: string, handler: (...args: unknown[]) => void) => void;
}

const RETRY_DELAYS_MS = [1_000, 2_000, 5_000, 10_000, 30_000];

const HUB_URL =
  (import.meta.env.VITE_SIGNALR_URL as string | undefined) ?? 'http://localhost:5000';

export function useSignalR({
  url = `${HUB_URL}/hubs/dashboard`,
  onConnected,
  skip = false,
}: UseSignalROptions = {}): UseSignalRReturn {
  const [status, setStatus] = useState<SignalRStatus>('connecting');
  const connectionRef = useRef<HubConnection | null>(null);
  const onConnectedRef = useRef(onConnected);
  onConnectedRef.current = onConnected;

  useEffect(() => {
    if (skip) {
      setStatus('disconnected');
      return;
    }

    const conn = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect(RETRY_DELAYS_MS)
      .configureLogging(
        import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning,
      )
      .build();

    connectionRef.current = conn;

    conn.onreconnecting(() => setStatus('reconnecting'));
    conn.onreconnected(() => {
      setStatus('connected');
      onConnectedRef.current?.(conn);
    });
    conn.onclose(() => setStatus('disconnected'));

    let cancelled = false;

    (async () => {
      try {
        await conn.start();
        if (!cancelled) {
          setStatus('connected');
          await onConnectedRef.current?.(conn);
        }
      } catch {
        if (!cancelled) setStatus('disconnected');
      }
    })();

    return () => {
      cancelled = true;
      conn.stop();
      connectionRef.current = null;
    };
    // url is intentionally the only dep — reconnect when hub URL changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [url, skip]);

  const on = useCallback(
    <TArgs extends unknown[]>(method: string, handler: (...args: TArgs) => void) => {
      connectionRef.current?.on(method, handler as (...args: unknown[]) => void);
    },
    [],
  );

  const off = useCallback((method: string, handler: (...args: unknown[]) => void) => {
    connectionRef.current?.off(method, handler);
  }, []);

  return { connection: connectionRef.current, status, on, off };
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers — server-invoke wrappers (fire-and-forget, swallow errors gracefully)
// ─────────────────────────────────────────────────────────────────────────────

export function invokeHub(
  connection: HubConnection | null,
  method: string,
  ...args: unknown[]
): void {
  if (connection?.state === HubConnectionState.Connected) {
    connection.invoke(method, ...args).catch((err: unknown) => {
      console.warn(`[SignalR] invoke "${method}" failed:`, err);
    });
  }
}
