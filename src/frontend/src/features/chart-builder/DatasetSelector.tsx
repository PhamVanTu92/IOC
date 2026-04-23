import { useDatasets } from '@/shared/hooks/useDatasets';
import type { ChartConfig } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// DatasetSelector — STEP 1: pick the data source
// ─────────────────────────────────────────────────────────────────────────────

interface DatasetSelectorProps {
  selectedId: string;
  onSelect: (id: string) => void;
  title: string;
  onTitleChange: (title: string) => void;
}

export function DatasetSelector({ selectedId, onSelect, title, onTitleChange }: DatasetSelectorProps) {
  const { datasets, loading, error } = useDatasets();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      {/* Chart title */}
      <div style={fieldGroupStyle}>
        <label style={labelStyle}>Tên biểu đồ</label>
        <input
          type="text"
          value={title}
          onChange={(e) => onTitleChange(e.target.value)}
          placeholder="VD: Doanh thu theo tháng"
          style={inputStyle}
        />
      </div>

      {/* Dataset list */}
      <div style={fieldGroupStyle}>
        <label style={labelStyle}>Nguồn dữ liệu (Dataset)</label>

        {loading && (
          <div style={hintStyle}>Đang tải danh sách dataset...</div>
        )}
        {error && (
          <div style={{ ...hintStyle, color: '#ef4444' }}>Lỗi: {error}</div>
        )}

        {!loading && datasets.length === 0 && (
          <div style={hintStyle}>Chưa có dataset nào. Hãy tạo dataset trước.</div>
        )}

        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {datasets.map((ds) => {
            const isSelected = ds.id === selectedId;
            return (
              <button
                key={ds.id}
                onClick={() => onSelect(ds.id)}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  padding: '12px 16px',
                  borderRadius: 8,
                  border: `2px solid ${isSelected ? '#3b82f6' : '#374151'}`,
                  backgroundColor: isSelected ? '#1e3a5f' : '#1f2937',
                  cursor: 'pointer',
                  textAlign: 'left',
                  transition: 'all 0.15s',
                }}
                onMouseEnter={(e) => {
                  if (!isSelected)
                    (e.currentTarget as HTMLButtonElement).style.borderColor = '#4b5563';
                }}
                onMouseLeave={(e) => {
                  if (!isSelected)
                    (e.currentTarget as HTMLButtonElement).style.borderColor = '#374151';
                }}
              >
                <div>
                  <div style={{ color: isSelected ? '#93c5fd' : '#e5e7eb', fontWeight: 600, fontSize: 14 }}>
                    {ds.name}
                  </div>
                  {ds.description && (
                    <div style={{ color: '#6b7280', fontSize: 12, marginTop: 2 }}>
                      {ds.description}
                    </div>
                  )}
                  <div style={{ color: '#4b5563', fontSize: 11, marginTop: 4 }}>
                    {ds.sourceType}
                    {!ds.isActive && (
                      <span style={{ marginLeft: 6, color: '#ef4444' }}>• Inactive</span>
                    )}
                  </div>
                </div>
                {isSelected && (
                  <span style={{ color: '#3b82f6', fontSize: 18 }}>✓</span>
                )}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ── Shared styles ─────────────────────────────────────────────────────────────

const fieldGroupStyle: React.CSSProperties = {
  display: 'flex',
  flexDirection: 'column',
  gap: 8,
};

const labelStyle: React.CSSProperties = {
  color: '#9ca3af',
  fontSize: 12,
  fontWeight: 600,
  letterSpacing: '0.05em',
  textTransform: 'uppercase',
};

const inputStyle: React.CSSProperties = {
  padding: '10px 12px',
  borderRadius: 6,
  border: '1px solid #374151',
  backgroundColor: '#111827',
  color: '#f9fafb',
  fontSize: 14,
  outline: 'none',
  width: '100%',
  boxSizing: 'border-box',
};

const hintStyle: React.CSSProperties = {
  color: '#6b7280',
  fontSize: 13,
  fontStyle: 'italic',
};
