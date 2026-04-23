import { useMemo } from 'react';
import type { QueryResultParsed } from '@/graphql/types';
import type { ChartConfig } from '@/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// KPIRenderer — single metric card with optional comparison/trend
// ─────────────────────────────────────────────────────────────────────────────

interface KPIRendererProps {
  data: QueryResultParsed;
  config: ChartConfig;
  height?: number;
  className?: string;
}

export function KPIRenderer({ data, config, height = 200, className }: KPIRendererProps) {
  const { rows, columns } = data;
  const { measures, metrics, visualOptions } = config;

  const valueField = useMemo(() => {
    const allFields = [...measures, ...metrics];
    if (allFields.length > 0 && columns.some((c) => c.name === allFields[0])) {
      return allFields[0];
    }
    // Fallback: first non-dimension numeric column
    return (
      columns.find(
        (c) =>
          c.fieldType !== 'dimension' &&
          (c.dataType === 'numeric' || c.dataType === 'integer' || c.dataType === 'float')
      )?.name ??
      columns[0]?.name ??
      ''
    );
  }, [measures, metrics, columns]);

  const comparisonField = useMemo(() => {
    const cf = visualOptions?.comparisonField;
    if (cf && columns.some((c) => c.name === cf)) return cf;
    const allFields = [...measures, ...metrics];
    if (allFields.length >= 2 && columns.some((c) => c.name === allFields[1])) {
      return allFields[1];
    }
    return null;
  }, [visualOptions, measures, metrics, columns]);

  const { value, comparisonValue } = useMemo(() => {
    if (rows.length === 0) return { value: null, comparisonValue: null };
    // Sum all rows for the main value field
    const sum = rows.reduce((acc, row) => {
      const v = row[valueField];
      return acc + (v !== null && v !== undefined ? Number(v) : 0);
    }, 0);

    const compSum = comparisonField
      ? rows.reduce((acc, row) => {
          const v = row[comparisonField];
          return acc + (v !== null && v !== undefined ? Number(v) : 0);
        }, 0)
      : null;

    return { value: sum, comparisonValue: compSum };
  }, [rows, valueField, comparisonField]);

  const col = columns.find((c) => c.name === valueField);
  const colLabel = col?.displayName ?? valueField;
  const prefix = visualOptions?.prefix ?? '';
  const suffix = visualOptions?.suffix ?? '';

  function formatValue(v: number | null): string {
    if (v === null) return '—';
    if (Math.abs(v) >= 1_000_000_000) return `${(v / 1_000_000_000).toFixed(1)}B`;
    if (Math.abs(v) >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`;
    if (Math.abs(v) >= 1_000) return `${(v / 1_000).toFixed(1)}K`;
    return v.toLocaleString('vi-VN', { maximumFractionDigits: 2 });
  }

  const percentChange =
    comparisonValue !== null && comparisonValue !== 0 && value !== null
      ? ((value - comparisonValue) / Math.abs(comparisonValue)) * 100
      : null;

  const trendColor =
    percentChange === null ? '#9ca3af' : percentChange >= 0 ? '#10b981' : '#ef4444';
  const trendIcon = percentChange === null ? '' : percentChange >= 0 ? '↑' : '↓';

  return (
    <div
      className={className}
      style={{
        height,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 24,
        textAlign: 'center',
        gap: 8,
      }}
    >
      {/* Label */}
      <div style={{ color: '#6b7280', fontSize: 13, fontWeight: 500, letterSpacing: '0.05em', textTransform: 'uppercase' }}>
        {colLabel}
      </div>

      {/* Main value */}
      <div
        style={{
          color: '#f9fafb',
          fontSize: Math.min(52, Math.max(32, height / 4)),
          fontWeight: 700,
          lineHeight: 1,
          fontVariantNumeric: 'tabular-nums',
        }}
      >
        {prefix}
        {formatValue(value)}
        {suffix}
      </div>

      {/* Comparison / trend */}
      {percentChange !== null && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span
            style={{
              color: trendColor,
              fontSize: 18,
              fontWeight: 700,
            }}
          >
            {trendIcon} {Math.abs(percentChange).toFixed(1)}%
          </span>
          <span style={{ color: '#4b5563', fontSize: 12 }}>vs comparison</span>
        </div>
      )}

      {/* Total rows indicator */}
      {rows.length > 1 && (
        <div style={{ color: '#4b5563', fontSize: 11 }}>
          Tổng hợp từ {rows.length} records
        </div>
      )}
    </div>
  );
}
