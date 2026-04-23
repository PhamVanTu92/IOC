import { useState } from 'react';
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  closestCenter,
  type DragStartEvent,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  rectSortingStrategy,
  arrayMove,
} from '@dnd-kit/sortable';
import { DashboardWidgetShell, DashboardWidgetGhost } from './DashboardWidgetShell';
import { useDashboardStore } from './useDashboardStore';
import type { DashboardWidget, WidgetSizePreset } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// DashboardGrid — 12-column drag & drop canvas
// Uses @dnd-kit/sortable for reordering;
// each widget spans its layout.w columns in CSS grid
// ─────────────────────────────────────────────────────────────────────────────

interface DashboardGridProps {
  /** Called when user clicks Edit on a widget */
  onEditWidget: (widgetId: string) => void;
}

export function DashboardGrid({ onEditWidget }: DashboardGridProps) {
  const widgets = useDashboardStore((s) => s.dashboard.widgets);
  const editMode = useDashboardStore((s) => s.editMode);
  const removeWidget = useDashboardStore((s) => s.removeWidget);
  const resizeWidget = useDashboardStore((s) => s.resizeWidget);
  const reorderWidgets = useDashboardStore((s) => s.reorderWidgets);

  const [activeId, setActiveId] = useState<string | null>(null);
  const activeWidget = activeId ? widgets.find((w) => w.id === activeId) ?? null : null;

  // ── dnd-kit sensors ────────────────────────────────────────────────────────
  const sensors = useSensors(
    useSensor(PointerSensor, {
      // Only start drag after 8px movement to avoid accidental drags
      activationConstraint: { distance: 8 },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  function handleDragStart({ active }: DragStartEvent) {
    setActiveId(active.id as string);
  }

  function handleDragEnd({ active, over }: DragEndEvent) {
    setActiveId(null);
    if (!over || active.id === over.id) return;

    const oldIndex = widgets.findIndex((w) => w.id === active.id);
    const newIndex = widgets.findIndex((w) => w.id === over.id);
    if (oldIndex === -1 || newIndex === -1) return;

    const reordered = arrayMove(widgets, oldIndex, newIndex);
    reorderWidgets(reordered.map((w) => w.id));
  }

  function handleResize(widgetId: string, preset: WidgetSizePreset) {
    resizeWidget(widgetId, preset);
  }

  function handleDelete(widgetId: string) {
    if (window.confirm('Xoá widget này khỏi dashboard?')) {
      removeWidget(widgetId);
    }
  }

  // ── Empty state ────────────────────────────────────────────────────────────
  if (widgets.length === 0) {
    return (
      <EmptyCanvas editMode={editMode} />
    );
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <SortableContext
        items={widgets.map((w) => w.id)}
        strategy={rectSortingStrategy}
      >
        <div style={gridContainerStyle}>
          {widgets.map((widget) => (
            <DashboardWidgetShell
              key={widget.id}
              widget={widget}
              editMode={editMode}
              onEdit={() => onEditWidget(widget.id)}
              onDelete={() => handleDelete(widget.id)}
              onResize={(preset) => handleResize(widget.id, preset)}
            />
          ))}
        </div>
      </SortableContext>

      {/* Drag ghost overlay */}
      <DragOverlay dropAnimation={{ duration: 180, easing: 'ease' }}>
        {activeWidget && <DashboardWidgetGhost widget={activeWidget} />}
      </DragOverlay>
    </DndContext>
  );
}

// ── Empty state ────────────────────────────────────────────────────────────────

function EmptyCanvas({ editMode }: { editMode: boolean }) {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 16,
        minHeight: 400,
        border: '2px dashed #1e293b',
        borderRadius: 12,
        color: '#374151',
        textAlign: 'center',
        padding: 48,
      }}
    >
      <span style={{ fontSize: 48 }}>📊</span>
      <div>
        <div style={{ color: '#4b5563', fontSize: 16, fontWeight: 600 }}>
          Dashboard trống
        </div>
        <div style={{ color: '#374151', fontSize: 13, marginTop: 6 }}>
          {editMode
            ? 'Thêm widget từ thư viện bên trái để bắt đầu'
            : 'Bật chế độ chỉnh sửa để thêm biểu đồ'}
        </div>
      </div>
    </div>
  );
}

// ── Styles ─────────────────────────────────────────────────────────────────────

const gridContainerStyle: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(12, 1fr)',
  gap: 12,
  alignItems: 'start',
};
