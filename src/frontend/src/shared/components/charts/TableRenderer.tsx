import { useState, useMemo } from 'react';
import type { QueryResultParsed } from '@/graphql/types';
import type { ChartConfig } from '@/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// TableRenderer — sortable, paginated data table (no ECharts)
// ─────────────────────────────────────────────────────────────────────────────

interface TableRendererProps {
  data: QueryResultParsed;
  config: ChartConfig;
  height?: number;
  className?: string;
}

type SortDir = 'asc' | 'desc' | null;
interface SortState { field: string; dir: SortDir }

const PAGE_SIZE = 20;

export function TableRenderer({ data, config, height = 400, className }: TableRendererProps) {
  const { rows, columns } = data;

  // Determine visible columns from config (dimensions + measures + metrics)
  // If none specified, show all
  const { dimensions, measures, metrics } = config;
  const requestedFields = [...dimensions, ...measures, ...metrics];
  const visibleCols =
    requestedFields.length > 0
      ? columns.filter((c) => requestedFields.includes(c.name))
      : columns;

  const [sort, setSort] = useState<SortState>({ field: '', dir: null });
  const [page, setPage] = useState(0);

  function toggleSort(field: string) {
    setSort((prev) => {
      if (prev.field !== field) return { field, dir: 'asc' };
      if (prev.dir === 'asc') return { field, dir: 'desc' };
      return { field: '', dir: null };
    });
    setPage(0);
  }

  const sorted = useMemo(() => {
    if (!sort.dir || !sort.field) return rows;
    return [...rows].sort((a, b) => {
      const av = a[sort.field];
      const bv = b[sort.field];
      if (av === null || av === undefined) return 1;
      if (bv === null || bv === undefined) return -1;
      const cmp =
        typeof av === 'number' && typeof bv === 'number'
          ? av - bv
          : String(av).localeCompare(String(bv), undefined, { numeric: true });
      return sort.dir === 'asc' ? cmp : -cmp;
    });
  }, [rows, sort]);

  const totalPages = Math.ceil(sorted.length / PAGE_SIZE);
  const pageRows = sorted.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);

  function formatCell(value: unknown, dataType: string): string {
    if (value === null || value === undefined) return '—';
    if (dataType === 'numeric' || dataType === 'float') {
      return Number(value).toLocaleString('vi-VN', { maximumFractionDigits: 2 });
    }
    if (dataType === 'integer') {
      return Number(value).toLocaleString('vi-VN');
    }
    return String(value);
  }

  return (
    <div
      className={className}
      style={{ height, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}
    >
      {/* Table */}
      <div style={{ flex: 1, overflowY: 'auto', overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
          <thead style={{ position: 'sticky', top: 0, zIndex: 1 }}>
            <tr style={{ backgroundColor: '#1f2937' }}>
              {visibleCols.map((col) => {
                const isActive = sort.field === col.name;
                return (
                  <th
                    key={col.name}
                    onClick={() => toggleSort(col.name)}
                    style={{
                      padding: '10px 12px',
                      textAlign: col.fieldType === 'dimension' ? 'left' : 'right',
                      color: isActive ? '#3b82f6' : '#9ca3af',
                      fontWeight: 600,
                      fontSize: 12,
                      letterSpacing: '0.05em',
                      textTransform: 'uppercase',
                      cursor: 'pointer',
                      userSelect: 'none',
                      whiteSpace: 'nowrap',
                      borderBottom: '1px solid #374151',
                    }}
                  >
                    {col.displayName ?? col.name}
                    {isActive && (
                      <span style={{ marginLeft: 4 }}>
                        {sort.dir === 'asc' ? '↑' : '↓'}
                      </span>
                    )}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {pageRows.map((row, rowIdx) => (
              <tr
                key={rowIdx}
                style={{
                  backgroundColor: rowIdx % 2 === 0 ? '#111827' : '#1a2332',
                  transition: 'background 0.1s',
                }}
                onMouseEnter={(e) =>
                  ((e.currentTarget as HTMLTableRowElement).style.backgroundColor = '#1e3a5f')
                }
                onMouseLeave={(e) =>
                  ((e.currentTarget as HTMLTableRowElement).style.backgroundColor =
                    rowIdx % 2 === 0 ? '#111827' : '#1a2332')
                }
              >
                {visibleCols.map((col) => (
                  <td
                    key={col.name}
                    style={{
                      padding: '8px 12px',
                      textAlign: col.fieldType === 'dimension' ? 'left' : 'right',
                      color: '#e5e7eb',
                      borderBottom: '1px solid #1f2937',
                      fontVariantNumeric: 'tabular-nums',
                    }}
                  >
                    {formatCell(row[col.name], col.dataType)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '8px 12px',
            borderTop: '1px solid #374151',
            fontSize: 12,
            color: '#6b7280',
            flexShrink: 0,
          }}
        >
          <span>
            {page * PAGE_SIZE + 1}–{Math.min((page + 1) * PAGE_SIZE, sorted.length)} / {sorted.length} rows
          </span>
          <div style={{ display: 'flex', gap: 4 }}>
            <button
              onClick={() => setPage((p) => Math.max(0, p - 1))}
              disabled={page === 0}
              style={pagerBtnStyle(page === 0)}
            >
              ←
            </button>
            <span style={{ padding: '4px 8px' }}>
              {page + 1} / {totalPages}
            </span>
            <button
              onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
              disabled={page >= totalPages - 1}
              style={pagerBtnStyle(page >= totalPages - 1)}
            >
              →
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function pagerBtnStyle(disabled: boolean): React.CSSProperties {
  return {
    padding: '4px 10px',
    borderRadius: 4,
    border: '1px solid #374151',
    backgroundColor: disabled ? '#111827' : '#1f2937',
    color: disabled ? '#4b5563' : '#e5e7eb',
    cursor: disabled ? 'not-allowed' : 'pointer',
    fontSize: 13,
  };
}
