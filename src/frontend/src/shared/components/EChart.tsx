import React, { useMemo, useRef } from 'react';
import ReactECharts from 'echarts-for-react';
import type { EChartsOption, EChartsReactProps } from 'echarts-for-react';

// ─────────────────────────────────────────────────────────────────────────────
// IOC ECharts Wrapper — chuẩn hoá theme và responsive
// ─────────────────────────────────────────────────────────────────────────────

// Màu palette mặc định của IOC
const IOC_COLORS = [
  '#3B82F6', '#10B981', '#F59E0B', '#EF4444',
  '#8B5CF6', '#EC4899', '#06B6D4', '#84CC16',
];

// Theme mặc định inject vào mọi chart
const DEFAULT_THEME_OVERRIDES: Partial<EChartsOption> = {
  color: IOC_COLORS,
  backgroundColor: 'transparent',
  textStyle: { fontFamily: 'Inter, system-ui, sans-serif', fontSize: 12 },
  grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
};

export interface EChartProps {
  option: EChartsOption;
  height?: string | number;
  loading?: boolean;
  className?: string;
  onEvents?: EChartsReactProps['onEvents'];
}

export function EChart({ option, height = 300, loading = false, className, onEvents }: EChartProps) {
  // Merge theme overrides với option — phải memo để tránh re-render
  const mergedOption = useMemo<EChartsOption>(
    () => ({
      ...DEFAULT_THEME_OVERRIDES,
      ...option,
      color: option.color ?? IOC_COLORS,
    }),
    [option]
  );

  return (
    <ReactECharts
      option={mergedOption}
      style={{ height, width: '100%' }}
      showLoading={loading}
      loadingOption={{ text: 'Đang tải...', color: '#3B82F6' }}
      opts={{ renderer: 'canvas' }}
      className={className}
      onEvents={onEvents}
    />
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Preset chart helpers

export function LineChart({
  title,
  xData,
  series,
  ...rest
}: {
  title?: string;
  xData: string[];
  series: Array<{ name: string; data: number[] }>;
} & Omit<EChartProps, 'option'>) {
  const option = useMemo<EChartsOption>(
    () => ({
      title: title ? { text: title, textStyle: { fontSize: 14, fontWeight: 600 } } : undefined,
      tooltip: { trigger: 'axis' },
      legend: { bottom: 0 },
      xAxis: { type: 'category', data: xData },
      yAxis: { type: 'value' },
      series: series.map((s) => ({ ...s, type: 'line', smooth: true })),
    }),
    [title, xData, series]
  );
  return <EChart option={option} {...rest} />;
}

export function BarChart({
  title,
  xData,
  series,
  ...rest
}: {
  title?: string;
  xData: string[];
  series: Array<{ name: string; data: number[] }>;
} & Omit<EChartProps, 'option'>) {
  const option = useMemo<EChartsOption>(
    () => ({
      title: title ? { text: title, textStyle: { fontSize: 14, fontWeight: 600 } } : undefined,
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      legend: { bottom: 0 },
      xAxis: { type: 'category', data: xData },
      yAxis: { type: 'value' },
      series: series.map((s) => ({ ...s, type: 'bar' })),
    }),
    [title, xData, series]
  );
  return <EChart option={option} {...rest} />;
}

export function PieChart({
  title,
  data,
  ...rest
}: {
  title?: string;
  data: Array<{ name: string; value: number }>;
} & Omit<EChartProps, 'option'>) {
  const option = useMemo<EChartsOption>(
    () => ({
      title: title ? { text: title, left: 'center', textStyle: { fontSize: 14, fontWeight: 600 } } : undefined,
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
      legend: { orient: 'horizontal', bottom: 0 },
      series: [{ type: 'pie', radius: ['40%', '70%'], data, emphasis: { itemStyle: { shadowBlur: 10 } } }],
    }),
    [title, data]
  );
  return <EChart option={option} {...rest} />;
}
