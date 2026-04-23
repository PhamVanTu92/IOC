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
// BarRenderer — handles 'bar' and 'bar_horizontal' chart types
// ─────────────────────────────────────────────────────────────────────────────

interface BarRendererProps {
  data: QueryResultParsed;
  config: ChartConfig;
  height?: number;
  className?: string;
}

export function BarRenderer({ data, config, height = 400, className }: BarRendererProps) {
  const { rows, columns } = data;
  const { chartType, dimensions, measures, metrics, visualOptions } = config;

  const isHorizontal = chartType === 'bar_horizontal';
  const isStacked = visualOptions?.stacked ?? false;
  const colors = resolveColors(visualOptions?.colorPalette);

  const option = useMemo(() => {
    const xCol = dimensions[0] ?? columns[0]?.name ?? '';
    const categories = rows.map((row) => String(row[xCol] ?? ''));

    const seriesFields = [...measures, ...metrics].filter((f) =>
      columns.some((c) => c.name === f)
    );
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
        type: 'bar',
        stack: isStacked ? 'total' : undefined,
        itemStyle: { color, borderRadius: isHorizontal ? [0, 4, 4, 0] : [4, 4, 0, 0] },
        label: visualOptions?.showDataLabels
          ? {
              show: true,
              position: isHorizontal ? 'right' : 'top',
              formatter: '{c}',
              fontSize: 11,
              color: '#9ca3af',
            }
          : undefined,
        data: rows.map((row) => {
          const v = row[field];
          return v !== null && v !== undefined ? Number(v) : null;
        }),
      };
    });

    const categoryAxis = {
      type: 'category' as const,
      data: categories,
      axisLabel: {
        ...IOC_AXIS_LABEL,
        rotate: !isHorizontal && categories.length > 8 ? 30 : 0,
      },
      axisLine: IOC_AXIS_LINE,
      name: isHorizontal ? visualOptions?.yAxisLabel : visualOptions?.xAxisLabel,
      nameLocation: 'end' as const,
      nameTextStyle: { color: '#9ca3af', fontSize: 11 },
    };

    const valueAxis = {
      type: 'value' as const,
      axisLabel: IOC_AXIS_LABEL,
      axisLine: IOC_AXIS_LINE,
      splitLine: IOC_SPLIT_LINE,
      name: isHorizontal ? visualOptions?.xAxisLabel : visualOptions?.yAxisLabel,
      nameLocation: 'end' as const,
      nameTextStyle: { color: '#9ca3af', fontSize: 11 },
    };

    return {
      color: colors,
      grid: IOC_GRID,
      tooltip: { ...IOC_TOOLTIP, trigger: 'axis' },
      legend: visualOptions?.showLegend !== false && series.length > 1 ? IOC_LEGEND : undefined,
      xAxis: isHorizontal ? valueAxis : categoryAxis,
      yAxis: isHorizontal ? categoryAxis : valueAxis,
      series,
    };
  }, [rows, columns, dimensions, measures, metrics, isHorizontal, isStacked, colors, visualOptions]);

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
