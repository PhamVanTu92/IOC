import ReactECharts from 'echarts-for-react';
import { useMemo } from 'react';
import type { QueryResultParsed } from '@/graphql/types';
import type { ChartConfig } from '@/features/chart-builder/types';
import { IOC_TOOLTIP, resolveColors } from './chartTheme';

// ─────────────────────────────────────────────────────────────────────────────
// PieRenderer — handles 'pie' and 'donut' chart types
// ─────────────────────────────────────────────────────────────────────────────

interface PieRendererProps {
  data: QueryResultParsed;
  config: ChartConfig;
  height?: number;
  className?: string;
}

export function PieRenderer({ data, config, height = 400, className }: PieRendererProps) {
  const { rows, columns } = data;
  const { chartType, dimensions, measures, metrics, visualOptions } = config;

  const isDonut = chartType === 'donut';
  const colors = resolveColors(visualOptions?.colorPalette);

  const option = useMemo(() => {
    const nameCol = dimensions[0] ?? columns.find((c) => c.fieldType === 'dimension')?.name ?? columns[0]?.name ?? '';
    const valueField =
      (measures[0] ?? metrics[0]) ??
      columns.find((c) => c.fieldType !== 'dimension')?.name ??
      columns[1]?.name ??
      '';

    const seriesData = rows.map((row, idx) => ({
      name: String(row[nameCol] ?? `Item ${idx + 1}`),
      value: row[valueField] !== null && row[valueField] !== undefined ? Number(row[valueField]) : 0,
    }));

    return {
      color: colors,
      tooltip: {
        ...IOC_TOOLTIP,
        trigger: 'item',
        formatter: '{b}: {c} ({d}%)',
      },
      legend: visualOptions?.showLegend !== false
        ? {
            type: 'scroll',
            orient: 'vertical',
            right: '5%',
            top: 'middle',
            textStyle: { color: '#6b7280', fontSize: 12 },
          }
        : undefined,
      series: [
        {
          name: columns.find((c) => c.name === valueField)?.displayName ?? valueField,
          type: 'pie',
          radius: isDonut ? ['45%', '70%'] : '65%',
          center: visualOptions?.showLegend !== false ? ['40%', '50%'] : ['50%', '50%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 6, borderColor: '#111827', borderWidth: 2 },
          label: visualOptions?.showDataLabels !== false
            ? {
                show: true,
                formatter: '{b}: {d}%',
                color: '#9ca3af',
                fontSize: 11,
              }
            : { show: false },
          labelLine: { lineStyle: { color: '#4b5563' } },
          data: seriesData,
        },
      ],
    };
  }, [rows, columns, dimensions, measures, metrics, isDonut, colors, visualOptions]);

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
