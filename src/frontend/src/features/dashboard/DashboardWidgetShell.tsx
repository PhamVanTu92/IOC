import { useState } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { ChartWidget } from '@/shared/components/ChartWidget';
import { WIDGET_SIZE_PRESETS, widgetHeightPx, type DashboardWidget, type WidgetSizePreset } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// DashboardWidgetShell — sortable wrapper for a ChartWidget
// Renders: drag handle, resize picker, edit/delete controls
// ─────────────────────────────────────────────────────────────────────────────

interface DashboardWidgetShellProps {
  widget: DashboardWidget;
  editMode: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onResize: (preset: WidgetSizePreset) => void;
}

const SIZE_PRESET_LABELS: { preset: WidgetSizePreset; label: string; icon: string }[] = [
  { preset: 'small',  label: 'Small',  icon: '▪' },
  { preset: 'medium', label: 'Medium', icon: '◾' },
  { preset: 'large',  label: 'Large',  icon: '◼' },
  { preset: 'wide',   label: 'Wide',   icon: '▬' },
  { preset: 'full',   label: 'Full',   icon: '█' },
];

export function DashboardWidgetShell({
  widget,
  editMode,
  onEdit,
  onDelete,
  onResize,
}: DashboardWidgetShellProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: widget.id, disabled: !editMode });

  const [showResize, setShowResize] = useState(false);
  const [hovered, setHovered] = useState(false);

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.45 : 1,
    gridColumn: `span ${widget.layout.w}`,
    position: 'relative',
    zIndex: isDragging ? 100 : 'auto',
  };

  const chartHeight = widgetHeightPx(widget.layout.h);

  return (
    <div
      ref={setNodeRef}
      style={style}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => { setHovered(false); setShowResize(false); }}
    >
      {/* Edit-mode overlay controls */}
      {editMode && (hovered || isDragging) && (
        <div
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            zIndex: 20,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '4px 6px',
            backgroundColor: '#0f172acc',
            borderRadius: '10px 10px 0 0',
            backdropFilter: 'blur(4px)',
          }}
        >
          {/* Drag handle */}
          <button
            {...attributes}
            {...listeners}
            style={{
              cursor: isDragging ? 'grabbing' : 'grab',
              color: '#6b7280',
              background: 'none',
              border: 'none',
              padding: '2px 6px',
              fontSize: 16,
              lineHeight: 1,
              display: 'flex',
              alignItems: 'center',
              gap: 4,
            }}
            title="Kéo để di chuyển"
          >
            ⠿ <span style={{ fontSize: 11, color: '#4b5563' }}>Kéo</span>
          </button>

          {/* Right-side actions */}
          <div style={{ display: 'flex', gap: 4, alignItems: 'center', position: 'relative' }}>
            {/* Resize picker toggle */}
            <ControlButton
              title="Thay đổi kích thước"
              onClick={() => setShowResize((v) => !v)}
              active={showResize}
            >
              ⊞
            </ControlButton>

            {/* Edit */}
            <ControlButton title="Chỉnh sửa biểu đồ" onClick={onEdit}>
              ✏️
            </ControlButton>

            {/* Delete */}
            <ControlButton title="Xoá widget" onClick={onDelete} danger>
              🗑️
            </ControlButton>

            {/* Resize dropdown */}
            {showResize && (
              <div
                style={{
                  position: 'absolute',
                  top: '110%',
                  right: 0,
                  zIndex: 50,
                  backgroundColor: '#1f2937',
                  border: '1px solid #374151',
                  borderRadius: 8,
                  padding: 6,
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 2,
                  minWidth: 130,
                  boxShadow: '0 8px 24px #000a',
                }}
              >
                {SIZE_PRESET_LABELS.map(({ preset, label, icon }) => {
                  const size = WIDGET_SIZE_PRESETS[preset];
                  const isActive =
                    widget.layout.w === size.w && widget.layout.h === size.h;
                  return (
                    <button
                      key={preset}
                      onClick={() => { onResize(preset); setShowResize(false); }}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        padding: '5px 10px',
                        borderRadius: 5,
                        border: 'none',
                        backgroundColor: isActive ? '#1e3a5f' : 'transparent',
                        color: isActive ? '#93c5fd' : '#9ca3af',
                        cursor: 'pointer',
                        fontSize: 12,
                        textAlign: 'left',
                      }}
                    >
                      <span>{icon} {label}</span>
                      <span style={{ color: '#4b5563', fontSize: 10 }}>
                        {size.w}×{size.h}
                      </span>
                    </button>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      )}

      {/* The actual chart */}
      <ChartWidget
        config={widget.chartConfig}
        height={chartHeight}
        showHeader={!editMode}
        showRefresh={!editMode}
        style={{
          borderRadius: 10,
          border: editMode
            ? `2px dashed ${hovered ? '#3b82f6' : '#374151'}`
            : '1px solid #1e293b',
          transition: 'border-color 0.15s',
        }}
      />
    </div>
  );
}

// ── Sub-component ─────────────────────────────────────────────────────────────

function ControlButton({
  children,
  title,
  onClick,
  active,
  danger,
}: {
  children: React.ReactNode;
  title: string;
  onClick: () => void;
  active?: boolean;
  danger?: boolean;
}) {
  return (
    <button
      title={title}
      onClick={onClick}
      style={{
        width: 26,
        height: 26,
        borderRadius: 5,
        border: `1px solid ${active ? '#3b82f6' : danger ? '#7f1d1d' : '#374151'}`,
        backgroundColor: active ? '#1e3a5f' : danger ? '#1c0a0a' : '#111827',
        color: active ? '#93c5fd' : danger ? '#fca5a5' : '#9ca3af',
        cursor: 'pointer',
        fontSize: 13,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 0,
        lineHeight: 1,
      }}
    >
      {children}
    </button>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// DragOverlay version — ghost rendered while dragging
// ─────────────────────────────────────────────────────────────────────────────

export function DashboardWidgetGhost({ widget }: { widget: DashboardWidget }) {
  const chartHeight = widgetHeightPx(widget.layout.h);
  return (
    <div
      style={{
        gridColumn: `span ${widget.layout.w}`,
        height: chartHeight,
        borderRadius: 10,
        border: '2px dashed #3b82f6',
        backgroundColor: '#172554',
        opacity: 0.7,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        color: '#3b82f6',
        fontSize: 13,
      }}
    >
      {widget.chartConfig.title}
    </div>
  );
}
