import { useEffect, useRef, useState } from 'react';
import { invokeHub, useSignalR } from './useSignalR';

// ─────────────────────────────────────────────────────────────────────────────
// useLiveMetrics — subscribe to realtime metric pushes for a business domain
//
// Usage:
//   const { metrics, status } = useLiveMetrics('finance');
//
// Each "ReceiveMetricUpdate" push merges into the metrics map keyed by
// metricName so the latest value is always available.
// ─────────────────────────────────────────────────────────────────────────────

export interface LiveMetric {
  datasetId: string;
  domain: string;
  metricName: string;
  value: number;
  unit: string;
  tenantId: string;
  timestamp: string;
}

export interface UseLiveMetricsReturn {
  /** Latest values keyed by metricName */
  metrics: Record<string, LiveMetric>;
  status: 'connecting' | 'connected' | 'reconnecting' | 'disconnected';
}

export function useLiveMetrics(domain: string): UseLiveMetricsReturn {
  const [metrics, setMetrics] = useState<Record<string, LiveMetric>>({});

  const { status, on, off } = useSignalR({
    skip: !domain,
    onConnected: (conn) => {
      // (Re-)subscribe after every reconnect
      conn.invoke('SubscribeToDomain', domain).catch((err: unknown) =>
        console.warn('[useLiveMetrics] SubscribeToDomain failed:', err),
      );
    },
  });

  // Keep domain ref to access inside event handler without stale closure
  const domainRef = useRef(domain);
  domainRef.current = domain;

  useEffect(() => {
    const handler = (metric: LiveMetric) => {
      if (metric.domain?.toLowerCase() !== domainRef.current?.toLowerCase()) return;
      setMetrics((prev) => ({ ...prev, [metric.metricName]: metric }));
    };

    on<[LiveMetric]>('ReceiveMetricUpdate', handler);
    return () => off('ReceiveMetricUpdate', handler as (...args: unknown[]) => void);
  }, [on, off]);

  // When domain changes, unsubscribe old and subscribe new
  useEffect(() => {
    setMetrics({}); // clear stale values when switching domain
  }, [domain]);

  return { metrics, status };
}

// ─────────────────────────────────────────────────────────────────────────────
// useDatasetRefresh — fires a callback whenever a dataset gets fresh data
//
// Usage inside ChartWidget:
//   useDatasetRefresh(config.datasetId, () => refetch());
// ─────────────────────────────────────────────────────────────────────────────

export interface DatasetRefreshedPayload {
  datasetId: string;
  cacheKey?: string;
  totalRows?: number;
  timestamp: string;
}

export function useDatasetRefresh(
  datasetId: string | undefined,
  onRefresh: (payload: DatasetRefreshedPayload) => void,
): void {
  const { on, off, connection } = useSignalR({
    skip: !datasetId,
    onConnected: (conn) => {
      if (datasetId) {
        conn
          .invoke('SubscribeToDataset', datasetId)
          .catch((err: unknown) =>
            console.warn('[useDatasetRefresh] SubscribeToDataset failed:', err),
          );
      }
    },
  });

  const onRefreshRef = useRef(onRefresh);
  onRefreshRef.current = onRefresh;

  useEffect(() => {
    const handler = (payload: DatasetRefreshedPayload) => {
      if (payload.datasetId === datasetId) {
        onRefreshRef.current(payload);
      }
    };

    on<[DatasetRefreshedPayload]>('DatasetRefreshed', handler);
    return () => off('DatasetRefreshed', handler as (...args: unknown[]) => void);
  }, [on, off, datasetId]);

  // Subscribe when connection becomes ready (covers late-mount)
  useEffect(() => {
    if (connection && datasetId) {
      invokeHub(connection, 'SubscribeToDataset', datasetId);
    }
  }, [connection, datasetId]);
}
