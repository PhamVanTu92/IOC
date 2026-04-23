import { useDataset } from '@/shared/hooks/useDatasets';
import type { ChartConfig, Granularity } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// FieldPicker — STEP 2: pick dimensions, measures, metrics, time settings
// ─────────────────────────────────────────────────────────────────────────────

interface FieldPickerProps {
  config: ChartConfig;
  onToggleDimension: (name: string) => void;
  onToggleMeasure: (name: string) => void;
  onToggleMetric: (name: string) => void;
  onSetTimeDimension: (name: string | undefined) => void;
  onSetGranularity: (g: ChartConfig['granularity']) => void;
  onSetLimit: (limit: number) => void;
}

const GRANULARITIES: { value: Granularity; label: string }[] = [
  { value: 'hour', label: 'Theo giờ' },
  { value: 'day', label: 'Theo ngày' },
  { value: 'week', label: 'Theo tuần' },
  { value: 'month', label: 'Theo tháng' },
  { value: 'quarter', label: 'Theo quý' },
  { value: 'year', label: 'Theo năm' },
];

export function FieldPicker({
  config,
  onToggleDimension,
  onToggleMeasure,
  onToggleMetric,
  onSetTimeDimension,
  onSetGranularity,
  onSetLimit,
}: FieldPickerProps) {
  const { dataset, loading, error } = useDataset(config.datasetId);

  if (loading) return <div style={hintStyle}>Đang tải metadata...</div>;
  if (error) return <div style={{ ...hintStyle, color: '#ef4444' }}>Lỗi: {error}</div>;
  if (!dataset) return <div style={hintStyle}>Không tìm thấy dataset.</div>;

  const timeDimensions = dataset.dimensions.filter((d) => d.isTimeDimension && d.isActive);
  const regularDimensions = dataset.dimensions.filter((d) => !d.isTimeDimension && d.isActive);
  const activeMeasures = dataset.measures.filter((m) => m.isActive);
  const activeMetrics = dataset.metrics.filter((m) => m.isActive);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      {/* Dimensions */}
      <FieldGroup
        title="Dimensions"
        hint="Các trường dùng để phân nhóm (GROUP BY)"
        badge={config.dimensions.length}
      >
        {regularDimensions.length === 0 && (
          <div style={emptyHint}>Không có dimension nào</div>
        )}
        {regularDimensions.map((dim) => (
          <FieldChip
            key={dim.id}
            name={dim.name}
            label={dim.displayName}
            description={dim.description}
            dataType={dim.dataType}
            isSelected={config.dimensions.includes(dim.name)}
            onToggle={() => onToggleDimension(dim.name)}
          />
        ))}
      </FieldGroup>

      {/* Measures */}
      <FieldGroup
        title="Measures"
        hint="Các chỉ số tổng hợp (SUM, AVG, COUNT...)"
        badge={config.measures.length}
      >
        {activeMeasures.length === 0 && (
          <div style={emptyHint}>Không có measure nào</div>
        )}
        {activeMeasures.map((m) => (
          <FieldChip
            key={m.id}
            name={m.name}
            label={m.displayName}
            description={m.description}
            dataType={m.dataType}
            aggregation={m.aggregationType}
            isSelected={config.measures.includes(m.name)}
            onToggle={() => onToggleMeasure(m.name)}
          />
        ))}
      </FieldGroup>

      {/* Metrics */}
      {activeMetrics.length > 0 && (
        <FieldGroup
          title="Metrics"
          hint="Công thức tính toán từ nhiều measures"
          badge={config.metrics.length}
        >
          {activeMetrics.map((m) => (
            <FieldChip
              key={m.id}
              name={m.name}
              label={m.displayName}
              description={m.description}
              dataType={m.dataType}
              isSelected={config.metrics.includes(m.name)}
              onToggle={() => onToggleMetric(m.name)}
            />
          ))}
        </FieldGroup>
      )}

      {/* Time dimension */}
      {timeDimensions.length > 0 && (
        <div style={fieldGroupStyle}>
          <label style={labelStyle}>Time Dimension (tuỳ chọn)</label>
          <select
            value={config.timeDimensionName ?? ''}
            onChange={(e) => onSetTimeDimension(e.target.value || undefined)}
            style={selectStyle}
          >
            <option value="">— Không dùng time dimension —</option>
            {timeDimensions.map((d) => (
              <option key={d.id} value={d.name}>
                {d.displayName}
              </option>
            ))}
          </select>

          {config.timeDimensionName && (
            <div style={{ marginTop: 8 }}>
              <label style={{ ...labelStyle, fontSize: 11 }}>Granularity</label>
              <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginTop: 4 }}>
                {GRANULARITIES.map((g) => (
                  <button
                    key={g.value}
                    onClick={() => onSetGranularity(g.value)}
                    style={{
                      padding: '4px 10px',
                      borderRadius: 4,
                      border: `1px solid ${config.granularity === g.value ? '#3b82f6' : '#374151'}`,
                      backgroundColor: config.granularity === g.value ? '#1e3a5f' : '#1f2937',
                      color: config.granularity === g.value ? '#93c5fd' : '#9ca3af',
                      fontSize: 12,
                      cursor: 'pointer',
                    }}
                  >
                    {g.label}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Row limit */}
      <div style={fieldGroupStyle}>
        <label style={labelStyle}>Giới hạn rows</label>
        <div style={{ display: 'flex', gap: 6 }}>
          {[100, 500, 1000, 5000].map((limit) => (
            <button
              key={limit}
              onClick={() => onSetLimit(limit)}
              style={{
                padding: '6px 14px',
                borderRadius: 4,
                border: `1px solid ${config.limit === limit ? '#3b82f6' : '#374151'}`,
                backgroundColor: config.limit === limit ? '#1e3a5f' : '#1f2937',
                color: config.limit === limit ? '#93c5fd' : '#9ca3af',
                fontSize: 13,
                cursor: 'pointer',
              }}
            >
              {limit.toLocaleString()}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

interface FieldGroupProps {
  title: string;
  hint?: string;
  badge?: number;
  children: React.ReactNode;
}

function FieldGroup({ title, hint, badge, children }: FieldGroupProps) {
  return (
    <div style={fieldGroupStyle}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <span style={labelStyle}>{title}</span>
        {badge !== undefined && badge > 0 && (
          <span
            style={{
              backgroundColor: '#1d4ed8',
              color: '#93c5fd',
              borderRadius: 10,
              padding: '1px 7px',
              fontSize: 11,
              fontWeight: 700,
            }}
          >
            {badge}
          </span>
        )}
      </div>
      {hint && <div style={{ color: '#4b5563', fontSize: 11, marginTop: -4 }}>{hint}</div>}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>{children}</div>
    </div>
  );
}

interface FieldChipProps {
  name: string;
  label: string;
  description?: string;
  dataType: string;
  aggregation?: string;
  isSelected: boolean;
  onToggle: () => void;
}

function FieldChip({ label, description, dataType, aggregation, isSelected, onToggle }: FieldChipProps) {
  return (
    <button
      onClick={onToggle}
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '8px 12px',
        borderRadius: 6,
        border: `1px solid ${isSelected ? '#3b82f6' : '#374151'}`,
        backgroundColor: isSelected ? '#172554' : '#111827',
        cursor: 'pointer',
        textAlign: 'left',
        transition: 'all 0.1s',
      }}
    >
      <div>
        <span style={{ color: isSelected ? '#93c5fd' : '#e5e7eb', fontWeight: 500, fontSize: 13 }}>
          {label}
        </span>
        {description && (
          <span style={{ color: '#4b5563', fontSize: 11, marginLeft: 8 }}>{description}</span>
        )}
      </div>
      <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
        {aggregation && (
          <span style={{ color: '#6b7280', fontSize: 10, backgroundColor: '#1f2937', padding: '1px 5px', borderRadius: 3 }}>
            {aggregation.toUpperCase()}
          </span>
        )}
        <span style={{ color: '#4b5563', fontSize: 10 }}>{dataType}</span>
        {isSelected && <span style={{ color: '#3b82f6', marginLeft: 4 }}>✓</span>}
      </div>
    </button>
  );
}

// ── Styles ─────────────────────────────────────────────────────────────────────

const fieldGroupStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: 8 };
const labelStyle: React.CSSProperties = {
  color: '#9ca3af', fontSize: 12, fontWeight: 600, letterSpacing: '0.05em', textTransform: 'uppercase',
};
const hintStyle: React.CSSProperties = { color: '#6b7280', fontSize: 13, fontStyle: 'italic', padding: 16 };
const emptyHint: React.CSSProperties = { color: '#4b5563', fontSize: 12, fontStyle: 'italic' };
const selectStyle: React.CSSProperties = {
  padding: '8px 12px', borderRadius: 6, border: '1px solid #374151',
  backgroundColor: '#111827', color: '#f9fafb', fontSize: 13, width: '100%',
};
