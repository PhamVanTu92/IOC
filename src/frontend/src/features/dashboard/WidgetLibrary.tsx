import { useState } from 'react';
import { CHART_TYPE_META, createDefaultConfig, type ChartType } from '@/features/chart-builder/types';
import { useDashboardStore } from './useDashboardStore';
import { WIDGET_SIZE_PRESETS, type WidgetSizePreset } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// WidgetLibrary — left sidebar panel for adding new widgets to the dashboard
// Two modes:
//   1. "Quick add" — pick a chart type → adds widget with placeholder config
//   2. "From builder" — opens full ChartBuilder modal (handled by parent)
// ─────────────────────────────────────────────────────────────────────────────

interface WidgetLibraryProps {
  onOpenBuilder: () => void;
}

const SIZE_OPTIONS: { preset: WidgetSizePreset; label: string }[] = [
  { preset: 'small',  label: 'Nhỏ (3×2)'  },
  { preset: 'medium', label: 'Vừa (6×3)'  },
  { preset: 'large',  label: 'Lớn (6×4)'  },
  { preset: 'wide',   label: 'Rộng (9×3)' },
  { preset: 'full',   label: 'Full (12×4)' },
];

export function WidgetLibrary({ onOpenBuilder }: WidgetLibraryProps) {
  const addWidget = useDashboardStore((s) => s.addWidget);
  const [selectedSize, setSelectedSize] = useState<WidgetSizePreset>('medium');

  function handleQuickAdd(type: ChartType) {
    const meta = CHART_TYPE_META.find((m) => m.type === type);
    const config = createDefaultConfig({
      chartType: type,
      title: meta?.label ?? type,
    });
    const size = WIDGET_SIZE_PRESETS[selectedSize];
    addWidget(config, { w: size.w, h: size.h });
  }

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: 16,
        padding: 16,
        backgroundColor: '#0f172a',
        border: '1px solid #1e293b',
        borderRadius: 10,
        height: '100%',
        overflowY: 'auto',
      }}
    >
      {/* Header */}
      <div>
        <div style={sectionLabel}>Thêm Widget</div>
        <div style={{ color: '#4b5563', fontSize: 11, marginTop: 2 }}>
          Chọn loại và thêm vào canvas
        </div>
      </div>

      {/* Size selector */}
      <div>
        <div style={{ ...sectionLabel, marginBottom: 6 }}>Kích thước mặc định</div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          {SIZE_OPTIONS.map(({ preset, label }) => (
            <button
              key={preset}
              onClick={() => setSelectedSize(preset)}
              style={{
                padding: '5px 10px',
                borderRadius: 5,
                border: `1px solid ${selectedSize === preset ? '#3b82f6' : '#1e293b'}`,
                backgroundColor: selectedSize === preset ? '#172554' : 'transparent',
                color: selectedSize === preset ? '#93c5fd' : '#6b7280',
                fontSize: 12,
                cursor: 'pointer',
                textAlign: 'left',
              }}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      {/* Quick-add chart types */}
      <div>
        <div style={{ ...sectionLabel, marginBottom: 6 }}>Loại biểu đồ</div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          {CHART_TYPE_META.map((meta) => (
            <button
              key={meta.type}
              onClick={() => handleQuickAdd(meta.type)}
              title={meta.description}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                padding: '7px 10px',
                borderRadius: 6,
                border: '1px solid #1e293b',
                backgroundColor: 'transparent',
                color: '#9ca3af',
                fontSize: 12,
                cursor: 'pointer',
                textAlign: 'left',
                transition: 'all 0.12s',
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.backgroundColor = '#1e293b';
                (e.currentTarget as HTMLButtonElement).style.color = '#e5e7eb';
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.backgroundColor = 'transparent';
                (e.currentTarget as HTMLButtonElement).style.color = '#9ca3af';
              }}
            >
              <span style={{ fontSize: 16, flexShrink: 0 }}>{meta.icon}</span>
              <span>{meta.label}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Divider */}
      <div style={{ borderTop: '1px solid #1e293b' }} />

      {/* Full builder button */}
      <button
        onClick={onOpenBuilder}
        style={{
          padding: '10px 14px',
          borderRadius: 8,
          border: '1px solid #1d4ed8',
          backgroundColor: '#1e3a5f',
          color: '#93c5fd',
          fontSize: 13,
          fontWeight: 600,
          cursor: 'pointer',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 6,
        }}
      >
        <span>✨</span>
        Mở Chart Builder
      </button>
      <div style={{ color: '#374151', fontSize: 11, textAlign: 'center', marginTop: -8 }}>
        Cấu hình dataset, fields, filters đầy đủ
      </div>
    </div>
  );
}

const sectionLabel: React.CSSProperties = {
  color: '#6b7280',
  fontSize: 11,
  fontWeight: 700,
  letterSpacing: '0.06em',
  textTransform: 'uppercase',
};
