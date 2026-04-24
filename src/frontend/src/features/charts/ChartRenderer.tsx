import { useMemo } from 'react';
import type { ChartConfig, ChartGql } from '@/graphql/moduleTypes';
import { EChart } from '@/shared/components/EChart';

// ─────────────────────────────────────────────────────────────────────────────
// ChartRenderer — renders the correct visualization based on chartType + config
// ─────────────────────────────────────────────────────────────────────────────

interface ChartRendererProps {
  chart: ChartGql;
  data?: Record<string, unknown>[];  // rows from datasource
  height?: number;
  loading?: boolean;
}

export function ChartRenderer({ chart, data = [], height = 300, loading }: ChartRendererProps) {
  const config: ChartConfig = useMemo(() => {
    try { return JSON.parse(chart.configJson) as ChartConfig; }
    catch { return {}; }
  }, [chart.configJson]);

  if (loading) return <ChartSkeleton height={height} />;

  switch (chart.chartType) {
    case 'kpi':     return <KPIRenderer config={config} data={data} />;
    case 'pie':     return <PieRenderer config={config} data={data} height={height} />;
    case 'table':   return <TableRenderer config={config} data={data} height={height} />;
    case 'line':    return <LineRenderer  config={config} data={data} height={height} />;
    case 'area':    return <AreaRenderer  config={config} data={data} height={height} />;
    case 'bar':
    case 'scatter':
    default:        return <BarRenderer   config={config} data={data} height={height} chartType={chart.chartType} />;
  }
}

// ── KPI ───────────────────────────────────────────────────────────────────────

function KPIRenderer({ config, data }: { config: ChartConfig; data: Record<string, unknown>[] }) {
  const field = config.valueField ?? config.yField ?? 'value';
  const value = data.length > 0 ? aggregateValues(data, field, config.aggregation ?? 'sum') : null;
  const formatted = value != null ? formatNumber(value) : '—';
  const unit = config.unit ?? '';
  const thresholds = config.thresholds;
  const color = thresholds && value != null
    ? value >= thresholds.danger ? '#ef4444'
    : value >= thresholds.warn   ? '#f59e0b'
    : '#22c55e'
    : '#0ea5e9';

  return (
    <div style={{ padding: '24px 16px', textAlign: 'center' }}>
      <div style={{ fontSize: 13, color: '#64748b', marginBottom: 8, fontWeight: 500 }}>
        {config.title ?? ''}
      </div>
      <div style={{ fontSize: 42, fontWeight: 800, color, letterSpacing: -1 }}>
        {formatted}
        {unit && <span style={{ fontSize: 18, marginLeft: 4, color: '#94a3b8' }}>{unit}</span>}
      </div>
    </div>
  );
}

// ── Bar / Scatter ─────────────────────────────────────────────────────────────

function BarRenderer({ config, data, height, chartType }: {
  config: ChartConfig; data: Record<string, unknown>[]; height: number; chartType: string;
}) {
  const xField = config.xField ?? 'name';
  const yField = config.yField ?? 'value';

  const option = useMemo(() => ({
    tooltip: { trigger: 'axis' as const },
    grid: { left: 40, right: 16, top: 24, bottom: 32 },
    xAxis: {
      type: 'category' as const,
      data: data.map(r => String(r[xField] ?? '')),
      axisLabel: { color: '#94a3b8', fontSize: 11 },
      axisLine: { lineStyle: { color: '#1e293b' } },
    },
    yAxis: {
      type: 'value' as const,
      axisLabel: { color: '#94a3b8', fontSize: 11 },
      splitLine: { lineStyle: { color: '#1e293b' } },
    },
    series: [{
      type: chartType === 'scatter' ? 'scatter' as const : 'bar' as const,
      data: data.map(r => Number(r[yField] ?? 0)),
      itemStyle: {
        color: config.colors?.[0] ?? '#0ea5e9',
        borderRadius: chartType === 'bar' ? [4, 4, 0, 0] : undefined,
      },
    }],
  }), [data, xField, yField, config.colors, chartType]);

  return <EChart option={option} height={height} />;
}

// ── Line ──────────────────────────────────────────────────────────────────────

function LineRenderer({ config, data, height }: { config: ChartConfig; data: Record<string, unknown>[]; height: number }) {
  const xField = config.xField ?? 'name';
  const yField = config.yField ?? 'value';

  const option = useMemo(() => ({
    tooltip: { trigger: 'axis' as const },
    grid: { left: 40, right: 16, top: 24, bottom: 32 },
    xAxis: {
      type: 'category' as const,
      data: data.map(r => String(r[xField] ?? '')),
      axisLabel: { color: '#94a3b8', fontSize: 11 },
      axisLine: { lineStyle: { color: '#1e293b' } },
    },
    yAxis: {
      type: 'value' as const,
      axisLabel: { color: '#94a3b8', fontSize: 11 },
      splitLine: { lineStyle: { color: '#1e293b' } },
    },
    series: [{
      type: 'line' as const,
      data: data.map(r => Number(r[yField] ?? 0)),
      smooth: true,
      symbol: 'circle',
      symbolSize: 6,
      lineStyle: { color: config.colors?.[0] ?? '#0ea5e9', width: 2 },
      itemStyle: { color: config.colors?.[0] ?? '#0ea5e9' },
    }],
  }), [data, xField, yField, config.colors]);

  return <EChart option={option} height={height} />;
}

// ── Area ──────────────────────────────────────────────────────────────────────

function AreaRenderer({ config, data, height }: { config: ChartConfig; data: Record<string, unknown>[]; height: number }) {
  const xField = config.xField ?? 'name';
  const yField = config.yField ?? 'value';
  const baseColor = config.colors?.[0] ?? '#0ea5e9';

  const option = useMemo(() => ({
    tooltip: { trigger: 'axis' as const },
    grid: { left: 40, right: 16, top: 24, bottom: 32 },
    xAxis: {
      type: 'category' as const,
      data: data.map(r => String(r[xField] ?? '')),
      axisLabel: { color: '#94a3b8', fontSize: 11 },
      axisLine: { lineStyle: { color: '#1e293b' } },
    },
    yAxis: {
      type: 'value' as const,
      axisLabel: { color: '#94a3b8', fontSize: 11 },
      splitLine: { lineStyle: { color: '#1e293b' } },
    },
    series: [{
      type: 'line' as const,
      data: data.map(r => Number(r[yField] ?? 0)),
      smooth: true,
      areaStyle: { color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [
        { offset: 0, color: baseColor + 'aa' },
        { offset: 1, color: baseColor + '11' },
      ]}},
      lineStyle: { color: baseColor, width: 2 },
      itemStyle: { color: baseColor },
    }],
  }), [data, xField, yField, baseColor]);

  return <EChart option={option} height={height} />;
}

// ── Pie ───────────────────────────────────────────────────────────────────────

function PieRenderer({ config, data, height }: { config: ChartConfig; data: Record<string, unknown>[]; height: number }) {
  const nameField  = config.nameField ?? config.xField ?? 'name';
  const valueField = config.valueField ?? config.yField ?? 'value';

  const option = useMemo(() => ({
    tooltip: { trigger: 'item' as const },
    legend: { bottom: 4, textStyle: { color: '#94a3b8', fontSize: 11 } },
    series: [{
      type: 'pie' as const,
      radius: ['40%', '70%'],
      center: ['50%', '45%'],
      data: data.map(r => ({ name: String(r[nameField] ?? ''), value: Number(r[valueField] ?? 0) })),
      itemStyle: { borderRadius: 4 },
      label: { color: '#94a3b8', fontSize: 11 },
    }],
  }), [data, nameField, valueField]);

  return <EChart option={option} height={height} />;
}

// ── Table ─────────────────────────────────────────────────────────────────────

function TableRenderer({ data, height }: { config: ChartConfig; data: Record<string, unknown>[]; height: number }) {
  if (data.length === 0) return <EmptyState />;
  const columns = Object.keys(data[0]);

  return (
    <div style={{ height, overflowY: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
        <thead>
          <tr>
            {columns.map(col => (
              <th key={col} style={{
                padding: '8px 12px', textAlign: 'left', color: '#64748b',
                borderBottom: '1px solid #1e293b', fontWeight: 600, position: 'sticky', top: 0,
                backgroundColor: '#0a1628',
              }}>
                {col}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((row, i) => (
            <tr key={i} style={{ borderBottom: '1px solid #1e293b' }}>
              {columns.map(col => (
                <td key={col} style={{ padding: '7px 12px', color: '#cbd5e1' }}>
                  {String(row[col] ?? '')}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function aggregateValues(
  data: Record<string, unknown>[],
  field: string,
  method: 'sum' | 'count' | 'avg' | 'max' | 'min',
): number {
  const vals = data.map(r => Number(r[field] ?? 0));
  switch (method) {
    case 'count': return data.length;
    case 'avg':   return vals.reduce((a, b) => a + b, 0) / (vals.length || 1);
    case 'max':   return Math.max(...vals);
    case 'min':   return Math.min(...vals);
    default:      return vals.reduce((a, b) => a + b, 0);
  }
}

function formatNumber(n: number): string {
  if (Math.abs(n) >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
  if (Math.abs(n) >= 1_000)     return (n / 1_000).toFixed(1) + 'K';
  return n.toLocaleString();
}

function ChartSkeleton({ height }: { height: number }) {
  return (
    <div style={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#1e293b' }}>
      <div style={{ width: '80%', height: '60%', background: '#1e293b', borderRadius: 8, animation: 'pulse 1.5s infinite' }} />
    </div>
  );
}

function EmptyState() {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', color: '#475569', fontSize: 13 }}>
      Không có dữ liệu
    </div>
  );
}
