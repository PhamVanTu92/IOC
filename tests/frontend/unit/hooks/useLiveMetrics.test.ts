import { renderHook, act, waitFor } from '@testing-library/react';
import { useLiveMetrics } from '../../../../src/frontend/src/shared/hooks/useLiveMetrics';

// ─────────────────────────────────────────────────────────────────────────────
// Minimal mock for useSignalR — lets us control on/off/status
// ─────────────────────────────────────────────────────────────────────────────

type Handler = (...args: unknown[]) => void;
const _handlers: Map<string, Handler[]> = new Map();
let _onConnectedCb: ((conn: unknown) => void) | undefined;

const mockConnection = {
  state: 'Connected',
  invoke: jest.fn().mockResolvedValue(undefined),
};

jest.mock('../../../../src/frontend/src/shared/hooks/useSignalR', () => ({
  useSignalR: jest.fn(({ onConnected }: { onConnected?: (conn: unknown) => void }) => {
    _onConnectedCb = onConnected;
    return {
      status: 'connected',
      connection: mockConnection,
      on: jest.fn((method: string, handler: Handler) => {
        if (!_handlers.has(method)) _handlers.set(method, []);
        _handlers.get(method)!.push(handler);
      }),
      off: jest.fn((method: string, handler: Handler) => {
        const list = _handlers.get(method) ?? [];
        _handlers.set(method, list.filter(h => h !== handler));
      }),
    };
  }),
  invokeHub: jest.fn(),
}));

function emitMetric(payload: Record<string, unknown>) {
  _handlers.get('ReceiveMetricUpdate')?.forEach(h => h(payload));
}

// ─────────────────────────────────────────────────────────────────────────────

describe('useLiveMetrics', () => {
  beforeEach(() => {
    _handlers.clear();
    _onConnectedCb = undefined;
    jest.clearAllMocks();
  });

  it('starts with empty metrics map', () => {
    const { result } = renderHook(() => useLiveMetrics('finance'));
    expect(result.current.metrics).toEqual({});
  });

  it('stores incoming metric under its metricName key', () => {
    const { result } = renderHook(() => useLiveMetrics('finance'));

    act(() => {
      emitMetric({
        datasetId: 'ds-1',
        domain: 'finance',
        metricName: 'revenue',
        value: 1_000_000,
        unit: 'VND',
        tenantId: 'tenant-1',
        timestamp: '2026-04-23T00:00:00Z',
      });
    });

    expect(result.current.metrics['revenue']).toBeDefined();
    expect(result.current.metrics['revenue'].value).toBe(1_000_000);
  });

  it('ignores metrics from other domains', () => {
    const { result } = renderHook(() => useLiveMetrics('finance'));

    act(() => {
      emitMetric({
        domain: 'hr',
        metricName: 'headcount',
        value: 200,
        unit: 'people',
        tenantId: 't1',
        timestamp: '2026-04-23T00:00:00Z',
      });
    });

    expect(result.current.metrics['headcount']).toBeUndefined();
  });

  it('merges multiple metrics and keeps latest value', () => {
    const { result } = renderHook(() => useLiveMetrics('finance'));

    act(() => {
      emitMetric({ domain: 'finance', metricName: 'revenue', value: 100, unit: 'VND', tenantId: 't', timestamp: '' });
      emitMetric({ domain: 'finance', metricName: 'revenue', value: 200, unit: 'VND', tenantId: 't', timestamp: '' });
      emitMetric({ domain: 'finance', metricName: 'cost', value: 50, unit: 'VND', tenantId: 't', timestamp: '' });
    });

    expect(result.current.metrics['revenue'].value).toBe(200);
    expect(result.current.metrics['cost'].value).toBe(50);
  });

  it('clears metrics when domain changes', async () => {
    const { result, rerender } = renderHook(
      ({ domain }: { domain: string }) => useLiveMetrics(domain),
      { initialProps: { domain: 'finance' } },
    );

    act(() => {
      emitMetric({ domain: 'finance', metricName: 'revenue', value: 100, unit: '', tenantId: '', timestamp: '' });
    });

    expect(result.current.metrics['revenue']).toBeDefined();

    rerender({ domain: 'hr' });

    await waitFor(() => {
      expect(result.current.metrics['revenue']).toBeUndefined();
    });
  });

  it('calls SubscribeToDomain on connect', async () => {
    renderHook(() => useLiveMetrics('marketing'));

    act(() => {
      _onConnectedCb?.(mockConnection);
    });

    await waitFor(() => {
      expect(mockConnection.invoke).toHaveBeenCalledWith('SubscribeToDomain', 'marketing');
    });
  });
});
