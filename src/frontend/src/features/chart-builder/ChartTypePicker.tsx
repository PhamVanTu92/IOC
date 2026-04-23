import { CHART_TYPE_META, type ChartType, type ChartConfig } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// ChartTypePicker — STEP 3: pick the chart type
// ─────────────────────────────────────────────────────────────────────────────

interface ChartTypePickerProps {
  config: ChartConfig;
  onSelect: (type: ChartType) => void;
}

export function ChartTypePicker({ config, onSelect }: ChartTypePickerProps) {
  const { dimensions, measures, metrics } = config;
  const totalFields = dimensions.length + measures.length + metrics.length;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ color: '#6b7280', fontSize: 13 }}>
        Chọn loại biểu đồ phù hợp với dữ liệu của bạn.
        {totalFields > 0 && (
          <span style={{ color: '#4b5563', marginLeft: 8 }}>
            ({dimensions.length} dim / {measures.length + metrics.length} measure)
          </span>
        )}
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))',
          gap: 12,
        }}
      >
        {CHART_TYPE_META.map((meta) => {
          const isSelected = config.chartType === meta.type;
          // Check if this chart type is compatible with selected fields
          const hasSufficientDims = dimensions.length >= meta.minDimensions;
          const hasSufficientMeasures =
            measures.length + metrics.length >= meta.minMeasures;
          const isCompatible = hasSufficientDims && hasSufficientMeasures;

          return (
            <button
              key={meta.type}
              onClick={() => onSelect(meta.type)}
              title={!isCompatible ? `Cần ít nhất ${meta.minDimensions} dim, ${meta.minMeasures} measure` : undefined}
              style={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: 8,
                padding: '16px 12px',
                borderRadius: 10,
                border: `2px solid ${isSelected ? '#3b82f6' : isCompatible ? '#374151' : '#1f2937'}`,
                backgroundColor: isSelected ? '#172554' : '#1f2937',
                cursor: 'pointer',
                opacity: isCompatible ? 1 : 0.45,
                transition: 'all 0.15s',
                position: 'relative',
              }}
              onMouseEnter={(e) => {
                if (!isSelected && isCompatible)
                  (e.currentTarget as HTMLButtonElement).style.borderColor = '#4b5563';
              }}
              onMouseLeave={(e) => {
                if (!isSelected)
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    isCompatible ? '#374151' : '#1f2937';
              }}
            >
              {/* Selected badge */}
              {isSelected && (
                <span
                  style={{
                    position: 'absolute',
                    top: 6,
                    right: 8,
                    color: '#3b82f6',
                    fontSize: 14,
                    fontWeight: 700,
                  }}
                >
                  ✓
                </span>
              )}

              {/* Icon */}
              <span style={{ fontSize: 28 }}>{meta.icon}</span>

              {/* Label */}
              <span
                style={{
                  color: isSelected ? '#93c5fd' : isCompatible ? '#e5e7eb' : '#4b5563',
                  fontSize: 13,
                  fontWeight: 600,
                  textAlign: 'center',
                }}
              >
                {meta.label}
              </span>

              {/* Description */}
              <span
                style={{
                  color: '#6b7280',
                  fontSize: 11,
                  textAlign: 'center',
                  lineHeight: 1.3,
                }}
              >
                {meta.description}
              </span>

              {/* Requirements */}
              {totalFields > 0 && !isCompatible && (
                <span style={{ color: '#ef4444', fontSize: 10, textAlign: 'center' }}>
                  Cần thêm trường
                </span>
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}
