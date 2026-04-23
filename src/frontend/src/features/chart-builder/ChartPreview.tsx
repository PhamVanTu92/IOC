import { useEffect } from 'react';
import { ChartRenderer } from '@/shared/components/charts/ChartRenderer';
import { useSemanticQuery } from '@/shared/hooks/useSemanticQuery';
import { isConfigValid, type ChartConfig } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// ChartPreview — STEP 4: live preview with data fetch + visual options panel
// ─────────────────────────────────────────────────────────────────────────────

interface ChartPreviewProps {
  config: ChartConfig;
  onVisualOptionChange?: (key: string, value: unknown) => void;
}

export function ChartPreview({ config }: ChartPreviewProps) {
  const valid = isConfigValid(config);

  const { data, loading, error, execute } = useSemanticQuery(config, {
    skip: !valid,
  });

  // Auto-execute when config changes and is valid
  useEffect(() => {
    if (valid) execute();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    config.datasetId,
    JSON.stringify(config.dimensions),
    JSON.stringify(config.measures),
    JSON.stringify(config.metrics),
    JSON.stringify(config.filters),
    JSON.stringify(config.sorts),
    config.limit,
    config.timeDimensionName,
    config.granularity,
    JSON.stringify(config.timeRange),
  ]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Config summary bar */}
      <div
        style={{
          display: 'flex',
          gap: 8,
          flexWrap: 'wrap',
          padding: '8px 12px',
          backgroundColor: '#111827',
          borderRadius: 8,
          border: '1px solid #1f2937',
        }}
      >
        <ConfigBadge label="Type" value={config.chartType} />
        {config.dimensions.length > 0 && (
          <ConfigBadge label="Dims" value={config.dimensions.join(', ')} />
        )}
        {config.measures.length > 0 && (
          <ConfigBadge label="Measures" value={config.measures.join(', ')} />
        )}
        {config.metrics.length > 0 && (
          <ConfigBadge label="Metrics" value={config.metrics.join(', ')} />
        )}
        {config.timeDimensionName && (
          <ConfigBadge
            label="Time"
            value={`${config.timeDimensionName}${config.granularity ? ` (${config.granularity})` : ''}`}
          />
        )}
        <ConfigBadge label="Limit" value={String(config.limit)} />
      </div>

      {/* Validation warning */}
      {!valid && (
        <div
          style={{
            padding: '12px 16px',
            borderRadius: 8,
            backgroundColor: '#451a03',
            border: '1px solid #92400e',
            color: '#fbbf24',
            fontSize: 13,
          }}
        >
          ⚠️ Cấu hình chưa đủ — cần chọn dataset và ít nhất một trường (dimension/measure/metric).
        </div>
      )}

      {/* Error message */}
      {error && (
        <div
          style={{
            padding: '12px 16px',
            borderRadius: 8,
            backgroundColor: '#450a0a',
            border: '1px solid #991b1b',
            color: '#fca5a5',
            fontSize: 13,
          }}
        >
          Lỗi truy vấn: {error}
        </div>
      )}

      {/* Preview area */}
      <div
        style={{
          borderRadius: 10,
          border: '1px solid #1f2937',
          backgroundColor: '#111827',
          overflow: 'hidden',
          minHeight: 400,
          position: 'relative',
        }}
      >
        {/* Title bar */}
        <div
          style={{
            padding: '12px 16px',
            borderBottom: '1px solid #1f2937',
            color: '#e5e7eb',
            fontWeight: 600,
            fontSize: 14,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <span>{config.title || 'Chart Preview'}</span>
          {loading && (
            <span style={{ color: '#6b7280', fontSize: 12, fontWeight: 400 }}>
              Đang tải dữ liệu...
            </span>
          )}
          {data && !loading && (
            <span style={{ color: '#4b5563', fontSize: 11, fontWeight: 400 }}>
              {data.metadata.totalRows.toLocaleString()} rows •{' '}
              {data.metadata.executionTimeMs}ms
              {data.metadata.fromCache && ' • cached'}
            </span>
          )}
        </div>

        {/* Loading overlay */}
        {loading && (
          <div
            style={{
              position: 'absolute',
              inset: 0,
              top: 49,
              backgroundColor: '#11182780',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              zIndex: 10,
            }}
          >
            <LoadingSpinner />
          </div>
        )}

        {/* Chart */}
        {data && (
          <ChartRenderer data={data} config={config} height={380} />
        )}

        {/* Empty / not queried */}
        {!loading && !data && !error && valid && (
          <div
            style={{
              height: 380,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#4b5563',
              fontSize: 13,
            }}
          >
            Nhấn "Chạy truy vấn" để xem preview
          </div>
        )}

        {/* Not valid */}
        {!valid && (
          <div
            style={{
              height: 380,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#374151',
              fontSize: 13,
            }}
          >
            Hoàn thiện cấu hình ở các bước trước
          </div>
        )}
      </div>

      {/* SQL inspector (only in dev) */}
      {import.meta.env.DEV && data?.metadata.generatedSql && (
        <details style={{ marginTop: 4 }}>
          <summary
            style={{ color: '#4b5563', fontSize: 12, cursor: 'pointer', userSelect: 'none' }}
          >
            Generated SQL
          </summary>
          <pre
            style={{
              marginTop: 8,
              padding: 12,
              borderRadius: 6,
              backgroundColor: '#0a0f1a',
              border: '1px solid #1f2937',
              color: '#86efac',
              fontSize: 11,
              overflowX: 'auto',
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-all',
            }}
          >
            {data.metadata.generatedSql}
          </pre>
        </details>
      )}
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function ConfigBadge({ label, value }: { label: string; value: string }) {
  return (
    <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
      <span style={{ color: '#4b5563', fontSize: 11 }}>{label}:</span>
      <span
        style={{
          color: '#93c5fd',
          fontSize: 11,
          fontWeight: 500,
          maxWidth: 140,
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          whiteSpace: 'nowrap',
        }}
      >
        {value}
      </span>
    </div>
  );
}

function LoadingSpinner() {
  return (
    <div
      style={{
        width: 32,
        height: 32,
        border: '3px solid #374151',
        borderTop: '3px solid #3b82f6',
        borderRadius: '50%',
        animation: 'spin 0.8s linear infinite',
      }}
    />
  );
}
