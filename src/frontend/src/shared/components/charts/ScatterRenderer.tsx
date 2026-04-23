import ReactECharts from 'echarts-for-react';
import { useMemo } from 'react';
import type { QueryResultParsed } from '@/graphql/types';
import type { ChartConfig } from '@/features/chart-builder/types';
import {
  IOC_GRID,
  IOC_TOOLTIP,
  IOC_LEGEND,
  IOC_AXIS_LABEL,
  IOC_AXIS_LINE,
  IOC_SPLIT_LINE,
  resolveColors,
} from './chartTheme';

// ─────────────────────────────────────────────────────────────────────────────
// ScatterRenderer — scatter plot: first 2 measures = X, Y axes
// ─────────────────────────────────────────────────────────────────────────────

interface ScatterRendererProps {
  data: QueryResultParsed;
  config: ChartConfig;
  height?: number;
  className?: string;
}

export function ScatterRenderer({ data, config, height = 400, className }: ScatterRendererProps) {
  const { rows, columns } = data;
  const { measures, metrics, dimensions, visualOptions } = config;

  const colors = resolveColors(visualOptions?.colorPalette);

  const option = useMemo(() => {
    const numericCols = columns.filter(
      (c) =>
        c.dataType === 'numeric' || c.dataType === 'integer' || c.dataType === 'float'
    );
    const allValueFields = [...measures, ...metrics].filter((f) =>
      columns.some((c) => c.name === f)
    );
    const activeFields =
      allValueFields.length >= 2 ? allValueFields : numericCols.map((c) => c.name);

    const xField = activeFields[0] ?? '';
    const yField = activeFields[1] ?? activeFields[0] ?? '';

    // Optional: color dimension (3rd measure or first dimension)
    const colorField = dimensions[0] ?? undefined;

    // Group by colorField if present
    const groups = new Map<string, Array<[number, number, string]>>();

    rows.forEach((row) => {
      const x = row[xField] !== null && row[xField] !== undefined ? Number(row[xField]) : null;
      const y = row[yField] !== null && row[yField] !== undefined ? Number(row[yField]) : null;
      if (x === null || y === null) return;

      const groupKey = colorField ? String(row[colorField] ?? 'Other') : 'Data';
      if (!groups.has(groupKey)) groups.set(groupKey, []);
      groups.get(groupKey)!.push([x, y, groupKey]);
    });

    const series = Array.from(groups.entries()).map(([groupName, points], idx) => ({
      name: groupName,
      type: 'scatter',
      symbolSize: 8,
      itemStyle: { color: colors[idx % colors.length], opacity: 0.75 },
      data: points.map(([x, y]) => [x, y]),
    }));

    const xCol = columns.find((c) => c.name === xField);
    const yCol = columns.find((c) => c.name === yField);

    return {
      color: colors,
      grid: IOC_GRID,
      tooltip: {
        ...IOC_TOOLTIP,
        trigger: 'item',
        formatter: (params: { seriesName: string; value: number[] }) =>
          `${params.seriesName}<br/>${xCol?.displayName ?? xField}: ${params.value[0]}<br/>${yCol?.displayName ?? yField}: ${params.value[1]}`,
      },
      legend: visualOptions?.showLegend !== false && groups.size > 1 ? IOC_LEGEND : undefined,
      xAxis: {
        type: 'value',
        name: visualOptions?.xAxisLabel ?? xCol?.displayName ?? xField,
        nameLocation: 'end',
        nameTextStyle: { color: '#9ca3af', fontSize: 11 },
        axisLabel: IOC_AXIS_LABEL,
        axisLine: IOC_AXIS_LINE,
        splitLine: IOC_SPLIT_LINE,
      },
      yAxis: {
        type: 'value',
        name: visualOptions?.yAxisLabel ?? yCol?.displayName ?? yField,
        nameLocation: 'end',
        nameTextStyle: { color: '#9ca3af', fontSize: 11 },
        axisLabel: IOC_AXIS_LABEL,
        axisLine: IOC_AXIS_LINE,
        splitLine: IOC_SPLIT_LINE,
      },
      series,
    };
  }, [rows, columns, measures, metrics, dimensions, colors, visualOptions]);

  return (
    <ReactECharts
      option={option}
      style={{ height, width: '100%' }}
      className={className}
      notMerge
      lazyUpdate
    />
  );
}
