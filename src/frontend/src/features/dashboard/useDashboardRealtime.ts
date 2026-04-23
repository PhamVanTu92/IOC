import { useEffect, useRef } from 'react';
import { invokeHub, useSignalR } from '../../shared/hooks/useSignalR';

// ─────────────────────────────────────────────────────────────────────────────
// useDashboardRealtime — subscribe to dashboard-level change events
//
// Listens for:
//   • "DashboardUpdated" — another user saved this dashboard (concurrent edit)
//   • "DashboardDeleted" — this dashboard was deleted
//
// Also subscribes at the tenant level to receive list updates (new dashboards,
// deleted dashboards) so the sidebar/list page can refresh automatically.
// ─────────────────────────────────────────────────────────────────────────────

export interface DashboardUpdatedPayload {
  dashboardId: string;
  title: string;
  widgetCount: number;
  savedBy: string;
  savedAt: string;
}

export interface DashboardDeletedPayload {
  dashboardId: string;
  deletedAt: string;
}

export interface UseDashboardRealtimeOptions {
  /** ID of the currently open dashboard (subscribe to concurrent edits). */
  dashboardId?: string;
  /** Tenant ID — subscribe to list-level events (new/deleted dashboards). */
  tenantId?: string;
  /** Called when another user saves this dashboard. */
  onUpdated?: (payload: DashboardUpdatedPayload) => void;
  /** Called when this dashboard is deleted remotely. */
  onDeleted?: (payload: DashboardDeletedPayload) => void;
  /** Called when any dashboard in the tenant changes (list refresh). */
  onTenantChange?: (payload: DashboardUpdatedPayload | DashboardDeletedPayload) => void;
}

export function useDashboardRealtime({
  dashboardId,
  tenantId,
  onUpdated,
  onDeleted,
  onTenantChange,
}: UseDashboardRealtimeOptions): void {
  const onUpdatedRef = useRef(onUpdated);
  const onDeletedRef = useRef(onDeleted);
  const onTenantChangeRef = useRef(onTenantChange);
  onUpdatedRef.current = onUpdated;
  onDeletedRef.current = onDeleted;
  onTenantChangeRef.current = onTenantChange;

  const { on, off, connection } = useSignalR({
    skip: !dashboardId && !tenantId,
    onConnected: async (conn) => {
      // Join all relevant groups on (re-)connect
      const joins: Promise<void>[] = [];
      if (dashboardId) joins.push(conn.invoke('SubscribeToDashboard', dashboardId));
      if (tenantId)    joins.push(conn.invoke('SubscribeToTenant', tenantId));
      await Promise.all(joins).catch((err: unknown) =>
        console.warn('[useDashboardRealtime] subscribe failed:', err),
      );
    },
  });

  // ── DashboardUpdated ───────────────────────────────────────────────────────
  useEffect(() => {
    const handler = (payload: DashboardUpdatedPayload) => {
      if (payload.dashboardId === dashboardId) {
        onUpdatedRef.current?.(payload);
      }
      onTenantChangeRef.current?.(payload);
    };

    on<[DashboardUpdatedPayload]>('DashboardUpdated', handler);
    return () => off('DashboardUpdated', handler as (...args: unknown[]) => void);
  }, [on, off, dashboardId]);

  // ── DashboardDeleted ───────────────────────────────────────────────────────
  useEffect(() => {
    const handler = (payload: DashboardDeletedPayload) => {
      if (payload.dashboardId === dashboardId) {
        onDeletedRef.current?.(payload);
      }
      onTenantChangeRef.current?.(payload);
    };

    on<[DashboardDeletedPayload]>('DashboardDeleted', handler);
    return () => off('DashboardDeleted', handler as (...args: unknown[]) => void);
  }, [on, off, dashboardId]);

  // ── Re-subscribe when IDs change ──────────────────────────────────────────
  useEffect(() => {
    if (connection && dashboardId) {
      invokeHub(connection, 'SubscribeToDashboard', dashboardId);
    }
  }, [connection, dashboardId]);

  useEffect(() => {
    if (connection && tenantId) {
      invokeHub(connection, 'SubscribeToTenant', tenantId);
    }
  }, [connection, tenantId]);
}
