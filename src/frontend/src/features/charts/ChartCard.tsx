import type { ChartGql } from '@/graphql/moduleTypes';
import { ChartRenderer } from './ChartRenderer';

interface ChartCardProps {
  chart: ChartGql;
  data?: Record<string, unknown>[];
  loading?: boolean;
  onEdit?: () => void;
  canEdit?: boolean;
}

export function ChartCard({ chart, data, loading, onEdit, canEdit }: ChartCardProps) {
  return (
    <div style={{
      backgroundColor: '#0f172a',
      border: '1px solid #1e293b',
      borderRadius: 10,
      overflow: 'hidden',
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
    }}>
      {/* Header */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '10px 14px',
        borderBottom: '1px solid #1e293b',
        flexShrink: 0,
      }}>
        <span style={{ fontSize: 13, fontWeight: 600, color: '#f1f5f9' }}>{chart.name}</span>
        {canEdit && onEdit && (
          <button
            onClick={onEdit}
            style={{
              background: 'none', border: '1px solid #1e293b', borderRadius: 5,
              color: '#64748b', fontSize: 11, padding: '2px 8px', cursor: 'pointer',
            }}
          >
            Sửa
          </button>
        )}
      </div>

      {/* Body */}
      <div style={{ flex: 1, padding: '8px 4px', minHeight: 0 }}>
        <ChartRenderer chart={chart} data={data} loading={loading} height={240} />
      </div>
    </div>
  );
}
