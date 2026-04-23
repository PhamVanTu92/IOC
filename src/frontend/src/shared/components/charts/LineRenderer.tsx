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
// LineRenderer — handles 'line' and 'area' chart types
// ─────────────────────────────────────────────────────────────────────────────

interface LineRendererProps {
  data: QueryResultParsed;
  config: ChartConfig;
  height?: number;
  className?: string;
}

export function LineRenderer({ data, config, height = 400, className }: LineRendererProps) {
  const { rows, columns } = data;
  const { chartType, dimensions, measures, metrics, visualOptions } = config;

  const isArea = chartType === 'area';
  const colors = resolveColors(visualOptions?.colorPalette);

  const option = useMemo(() => {
    // X-axis: first dimension column
    const xCol = dimensions[0] ?? columns[0]?.name ?? '';
    const xData = rows.map((row) => String(row[xCol] ?? ''));

    // Series: measure + metric columns
    const seriesFields = [
      ...measures,
      ...metrics,
    ].filter((f) => columns.some((c) => c.name === f));

    // Fallback: if no explicit measures/metrics, use all non-dimension numeric columns
    const numericCols = columns.filter(
      (c) =>
        c.fieldType !== 'dimension' &&
        !dimensions.includes(c.name) &&
        (c.dataType === 'numeric' || c.dataType === 'integer' || c.dataType === 'float')
    );
    const activeSeries =
      seriesFields.length > 0 ? seriesFields : numericCols.map((c) => c.name);

    const series = activeSeries.map((field, idx) => {
      const col = columns.find((c) => c.name === field);
      const color = colors[idx % colors.length];
      return {
        name: col?.displayName ?? field,
        type: 'line',
        smooth: visualOptions?.smooth ?? false,
        areaStyle: isArea ? { color: `${color}33` } : undefined,
        lineStyle: { color, width: 2 },
        itemStyle: { color },
        data: rows.map((row) => {
          const v = row[field];
          return v !== null && v !== undefined ? Number(v) : null;
        }),
      };
    });

    return {
      color: colors,
      grid: IOC_GRID,
      tooltip: {
        ...IOC_TOOLTIP,
        trigger: 'axis',
      },
      legend: visualOptions?.showLegend !== false && series.length > 1 ? IOC_LEGEND : undefined,
      xAxis: {
        type: 'category',
        data: xData,
        axisLabel: {
          ...IOC_AXIS_LABEL,
          rotate: xData.length > 12 ? 30 : 0,
        },
        axisLine: IOC_AXIS_LINE,
        name: visualOptions?.xAxisLabel,
        nameLocation: 'end',
        nameTextStyle: { color: '#9ca3af', fontSize: 11 },
      },
      yAxis: {
        type: 'value',
        axisLabel: IOC_AXIS_LABEL,
        axisLine: IOC_AXIS_LINE,
        splitLine: IOC_SPLIT_LINE,
        name: visualOptions?.yAxisLabel,
        nameLocation: 'end',
        nameTextStyle: { color: '#9ca3af', fontSize: 11 },
      },
      series,
    };
  }, [rows, columns, dimensions, measures, metrics, isArea, colors, visualOptions]);

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
