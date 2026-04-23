import type { QueryResultParsed } from '@/graphql/types';
import type { ChartConfig } from '@/features/chart-builder/types';
import { LineRenderer } from './LineRenderer';
import { BarRenderer } from './BarRenderer';
import { PieRenderer } from './PieRenderer';
import { TableRenderer } from './TableRenderer';
import { KPIRenderer } from './KPIRenderer';
import { ScatterRenderer } from './ScatterRenderer';

// ─────────────────────────────────────────────────────────────────────────────
// ChartRenderer — dispatcher component, routes to specific renderer by chartType
// ─────────────────────────────────────────────────────────────────────────────

interface ChartRendererProps {
  data: QueryResultParsed;
  config: ChartConfig;
  height?: number;
  className?: string;
}

export function ChartRenderer({ data, config, height = 400, className }: ChartRendererProps) {
  const { chartType } = config;

  if (data.rows.length === 0) {
    return (
      <div
        className={className}
        style={{
          height,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: '#6b7280',
          fontSize: 14,
        }}
      >
        Không có dữ liệu
      </div>
    );
  }

  const sharedProps = { data, config, height, className };

  switch (chartType) {
    case 'line':
    case 'area':
      return <LineRenderer {...sharedProps} />;

    case 'bar':
    case 'bar_horizontal':
      return <BarRenderer {...sharedProps} />;

    case 'pie':
    case 'donut':
      return <PieRenderer {...sharedProps} />;

    case 'scatter':
      return <ScatterRenderer {...sharedProps} />;

    case 'table':
      return <TableRenderer {...sharedProps} />;

    case 'kpi':
      return <KPIRenderer {...sharedProps} />;

    case 'heatmap':
      // Fallback — heatmap renderer future work
      return (
        <div
          className={className}
          style={{
            height,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#6b7280',
            fontSize: 14,
          }}
        >
          Heatmap renderer đang phát triển
        </div>
      );

    default: {
      const _exhaustive: never = chartType;
      console.warn('Unknown chartType:', _exhaustive);
      return null;
    }
  }
}
