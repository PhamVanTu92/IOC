import { renderHook, act } from '@testing-library/react';
import { useDashboardRealtime } from '../../../../src/frontend/src/features/dashboard/useDashboardRealtime';

// ─────────────────────────────────────────────────────────────────────────────
// Mock useSignalR
// ─────────────────────────────────────────────────────────────────────────────

type Handler = (...args: unknown[]) => void;
const _handlers: Map<string, Handler[]> = new Map();

const mockConnection = {
  state: 'Connected',
  invoke: jest.fn().mockResolvedValue(undefined),
};

jest.mock('../../../../src/frontend/src/shared/hooks/useSignalR', () => ({
  useSignalR: jest.fn(({ onConnected }: { onConnected?: (c: unknown) => void }) => {
    // Immediately simulate connected and call onConnected
    Promise.resolve().then(() => onConnected?.(mockConnection));
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

function emitEvent(method: string, payload: Record<string, unknown>) {
  _handlers.get(method)?.forEach(h => h(payload));
}

// ─────────────────────────────────────────────────────────────────────────────

describe('useDashboardRealtime', () => {
  beforeEach(() => {
    _handlers.clear();
    jest.clearAllMocks();
  });

  it('calls onUpdated when DashboardUpdated fires for matching dashboardId', () => {
    const onUpdated = jest.fn();
    const dashboardId = 'dash-1';

    renderHook(() =>
      useDashboardRealtime({ dashboardId, onUpdated }),
    );

    act(() => {
      emitEvent('DashboardUpdated', {
        dashboardId,
        title: 'Updated Dashboard',
        widgetCount: 3,
        savedBy: 'user@ioc.vn',
        savedAt: new Date().toISOString(),
      });
    });

    expect(onUpdated).toHaveBeenCalledTimes(1);
    expect(onUpdated.mock.calls[0][0]).toMatchObject({ dashboardId });
  });

  it('does not call onUpdated for a different dashboardId', () => {
    const onUpdated = jest.fn();

    renderHook(() =>
      useDashboardRealtime({ dashboardId: 'dash-1', onUpdated }),
    );

    act(() => {
      emitEvent('DashboardUpdated', {
        dashboardId: 'dash-OTHER',
        title: 'Other',
        widgetCount: 0,
        savedBy: 'other@ioc.vn',
        savedAt: new Date().toISOString(),
      });
    });

    expect(onUpdated).not.toHaveBeenCalled();
  });

  it('calls onDeleted when DashboardDeleted fires for matching dashboardId', () => {
    const onDeleted = jest.fn();
    const dashboardId = 'dash-42';

    renderHook(() =>
      useDashboardRealtime({ dashboardId, onDeleted }),
    );

    act(() => {
      emitEvent('DashboardDeleted', {
        dashboardId,
        deletedAt: new Date().toISOString(),
      });
    });

    expect(onDeleted).toHaveBeenCalledTimes(1);
    expect(onDeleted.mock.calls[0][0]).toMatchObject({ dashboardId });
  });

  it('calls onTenantChange for any DashboardUpdated event', () => {
    const onTenantChange = jest.fn();

    renderHook(() =>
      useDashboardRealtime({ dashboardId: 'dash-x', tenantId: 't1', onTenantChange }),
    );

    act(() => {
      emitEvent('DashboardUpdated', {
        dashboardId: 'dash-OTHER',   // different dashboard — still fires onTenantChange
        title: 'Another',
        widgetCount: 1,
        savedBy: 'u@ioc.vn',
        savedAt: new Date().toISOString(),
      });
    });

    expect(onTenantChange).toHaveBeenCalledTimes(1);
  });

  it('calls onTenantChange for DashboardDeleted event', () => {
    const onTenantChange = jest.fn();

    renderHook(() =>
      useDashboardRealtime({ dashboardId: 'dash-x', onTenantChange }),
    );

    act(() => {
      emitEvent('DashboardDeleted', {
        dashboardId: 'dash-GONE',
        deletedAt: new Date().toISOString(),
      });
    });

    expect(onTenantChange).toHaveBeenCalledTimes(1);
  });
});
