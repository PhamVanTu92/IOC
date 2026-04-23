import { useEffect, useRef, useState } from 'react';
import { ChartRenderer } from './charts/ChartRenderer';
import { useSemanticQuery } from '@/shared/hooks/useSemanticQuery';
import { isConfigValid, type ChartConfig } from '@/features/chart-builder/types';
import { useDatasetRefresh } from '@/shared/hooks/useLiveMetrics';

// ─────────────────────────────────────────────────────────────────────────────
// ChartWidget — self-contained, drop-in widget
//   Takes a ChartConfig, fetches data, renders the chart.
//   Used both in the dashboard canvas and in embed scenarios.
// ─────────────────────────────────────────────────────────────────────────────

interface ChartWidgetProps {
  config: ChartConfig;
  height?: number;
  /** Show the title bar */
  showHeader?: boolean;
  /** Show refresh button in header */
  showRefresh?: boolean;
  /** Callback when the edit icon is clicked */
  onEdit?: (config: ChartConfig) => void;
  /** Additional wrapper class */
  className?: string;
  style?: React.CSSProperties;
}

export function ChartWidget({
  config,
  height = 320,
  showHeader = true,
  showRefresh = true,
  onEdit,
  className,
  style,
}: ChartWidgetProps) {
  const valid = isConfigValid(config);
  const { data, loading, error, execute } = useSemanticQuery(config, { skip: !valid });

  // Execute on mount and when config changes (stable ref comparison)
  const configKey = useRef('');
  const currentKey = [
    config.datasetId,
    config.dimensions.join(','),
    config.measures.join(','),
    config.metrics.join(','),
    config.limit,
    config.timeDimensionName ?? '',
    config.granularity ?? '',
    JSON.stringify(config.timeRange),
    JSON.stringify(config.filters),
    JSON.stringify(config.sorts),
  ].join('|');

  useEffect(() => {
    if (!valid) return;
    if (configKey.current !== currentKey) {
      configKey.current = currentKey;
      execute();
    }
  });

  // Auto-refresh when the backend pushes a DatasetRefreshed event via SignalR
  useDatasetRefresh(valid ? config.datasetId : undefined, () => {
    execute();
  });

  const [hovered, setHovered] = useState(false);
  const headerHeight = showHeader ? 44 : 0;
  const chartHeight = height - headerHeight;

  return (
    <div
      className={className}
      style={{
        display: 'flex',
        flexDirection: 'column',
        height,
        borderRadius: 10,
        border: '1px solid #1e293b',
        backgroundColor: '#0f172a',
        overflow: 'hidden',
        position: 'relative',
        ...style,
      }}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      {/* Header */}
      {showHeader && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 14px',
            height: headerHeight,
            borderBottom: '1px solid #1e293b',
            flexShrink: 0,
          }}
        >
          <span
            style={{
              color: '#e5e7eb',
              fontSize: 13,
              fontWeight: 600,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              maxWidth: '70%',
            }}
          >
            {config.title}
          </span>

          <div
            style={{
              display: 'flex',
              gap: 4,
              opacity: hovered ? 1 : 0,
              transition: 'opacity 0.15s',
            }}
          >
            {showRefresh && (
              <IconButton
                title="Làm mới"
                onClick={() => execute()}
                disabled={loading || !valid}
              >
                🔄
              </IconButton>
            )}
            {onEdit && (
              <IconButton title="Chỉnh sửa" onClick={() => onEdit(config)}>
                ✏️
              </IconButton>
            )}
          </div>
        </div>
      )}

      {/* Body */}
      <div style={{ flex: 1, position: 'relative', overflow: 'hidden' }}>
        {/* Loading overlay */}
        {loading && (
          <div
            style={{
              position: 'absolute',
              inset: 0,
              backgroundColor: '#0f172a99',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              zIndex: 5,
            }}
          >
            <Spinner />
          </div>
        )}

        {/* Error state */}
        {error && !loading && (
          <div
            style={{
              height: chartHeight,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 8,
              padding: 16,
              color: '#fca5a5',
              fontSize: 12,
              textAlign: 'center',
            }}
          >
            <span style={{ fontSize: 22 }}>⚠️</span>
            <span>{error}</span>
            {valid && (
              <button
                onClick={() => execute()}
                style={{
                  marginTop: 4,
                  padding: '4px 12px',
                  borderRadius: 4,
                  border: '1px solid #991b1b',
                  backgroundColor: 'transparent',
                  color: '#fca5a5',
                  fontSize: 11,
                  cursor: 'pointer',
                }}
              >
                Thử lại
              </button>
            )}
          </div>
        )}

        {/* Not configured */}
        {!valid && (
          <div
            style={{
              height: chartHeight,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#374151',
              fontSize: 12,
            }}
          >
            Chưa cấu hình
          </div>
        )}

        {/* Chart */}
        {data && !error && (
          <ChartRenderer data={data} config={config} height={chartHeight} />
        )}

        {/* No data */}
        {valid && !loading && !error && !data && (
          <div
            style={{
              height: chartHeight,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#374151',
              fontSize: 12,
            }}
          >
            Không có dữ liệu
          </div>
        )}
      </div>

      {/* Footer metadata strip (only on hover) */}
      {data && hovered && (
        <div
          style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            padding: '3px 12px',
            backgroundColor: '#0f172acc',
            borderTop: '1px solid #1e293b',
            display: 'flex',
            justifyContent: 'flex-end',
            gap: 8,
            fontSize: 10,
            color: '#4b5563',
          }}
        >
          <span>{data.metadata.totalRows.toLocaleString()} rows</span>
          <span>•</span>
          <span>{data.metadata.executionTimeMs}ms</span>
          {data.metadata.fromCache && (
            <>
              <span>•</span>
              <span style={{ color: '#22c55e' }}>cached</span>
            </>
          )}
        </div>
      )}
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function IconButton({
  children,
  title,
  onClick,
  disabled,
}: {
  children: React.ReactNode;
  title: string;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      title={title}
      onClick={onClick}
      disabled={disabled}
      style={{
        width: 26,
        height: 26,
        borderRadius: 4,
        border: '1px solid #374151',
        backgroundColor: '#1f2937',
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.5 : 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontSize: 12,
        padding: 0,
      }}
    >
      {children}
    </button>
  );
}

function Spinner() {
  return (
    <div
      style={{
        width: 24,
        height: 24,
        borderRadius: '50%',
        border: '2px solid #1f2937',
        borderTop: '2px solid #3b82f6',
        animation: 'spin 0.7s linear infinite',
      }}
    />
  );
}
