import React, { useState, useCallback, useMemo } from 'react';
import {
  DndContext,
  DragOverlay,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragStartEvent,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  rectSortingStrategy,
  sortableKeyboardCoordinates,
  arrayMove,
} from '@dnd-kit/sortable';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { pluginRegistry, type WidgetConfig } from './PluginRegistry';

// ─────────────────────────────────────────────────────────────────────────────

interface DashboardItem {
  id: string;
  widgetId: string;
  config?: Record<string, unknown>;
}

interface SortableWidgetProps {
  item: DashboardItem;
  widget: WidgetConfig;
  isEditing: boolean;
  onRemove: (id: string) => void;
}

function SortableWidget({ item, widget, isEditing, onRemove }: SortableWidgetProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: item.id });

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  const WidgetComponent = widget.component;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`ioc-widget ioc-widget--${item.widgetId} ${isDragging ? 'ioc-widget--dragging' : ''}`}
    >
      {isEditing && (
        <div className="ioc-widget__toolbar" {...attributes} {...listeners}>
          <span className="ioc-widget__drag-handle">⠿</span>
          <span className="ioc-widget__title">{widget.name}</span>
          <button
            className="ioc-widget__remove"
            onClick={() => onRemove(item.id)}
            aria-label="Xoá widget"
          >
            ✕
          </button>
        </div>
      )}
      <div className="ioc-widget__content">
        <WidgetComponent config={item.config} isEditing={isEditing} />
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────

interface WidgetPaletteProps {
  onAddWidget: (widgetId: string) => void;
}

function WidgetPalette({ onAddWidget }: WidgetPaletteProps) {
  const allWidgets = pluginRegistry.getAllWidgets();
  const categories = ['kpi', 'chart', 'table', 'custom'] as const;

  return (
    <div className="ioc-palette">
      <h3 className="ioc-palette__title">Widget Library</h3>
      {categories.map((cat) => {
        const widgets = allWidgets.filter((w) => w.category === cat);
        if (widgets.length === 0) return null;
        return (
          <div key={cat} className="ioc-palette__category">
            <p className="ioc-palette__category-title">{cat.toUpperCase()}</p>
            {widgets.map((widget) => (
              <button
                key={widget.id}
                className="ioc-palette__item"
                onClick={() => onAddWidget(widget.id)}
                title={widget.description}
              >
                <span>+ {widget.name}</span>
              </button>
            ))}
          </div>
        );
      })}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────

export function DashboardBuilder() {
  const [items, setItems] = useState<DashboardItem[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [showPalette, setShowPalette] = useState(false);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const allWidgets = useMemo(() => pluginRegistry.getAllWidgets(), []);

  const getWidget = useCallback(
    (widgetId: string) => allWidgets.find((w) => w.id === widgetId),
    [allWidgets]
  );

  const handleDragStart = useCallback((event: DragStartEvent) => {
    setActiveId(event.active.id as string);
  }, []);

  const handleDragEnd = useCallback((event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      setItems((prev) => {
        const oldIndex = prev.findIndex((i) => i.id === active.id);
        const newIndex = prev.findIndex((i) => i.id === over.id);
        return arrayMove(prev, oldIndex, newIndex);
      });
    }
    setActiveId(null);
  }, []);

  const handleAddWidget = useCallback((widgetId: string) => {
    const newItem: DashboardItem = {
      id: `${widgetId}-${Date.now()}`,
      widgetId,
    };
    setItems((prev) => [...prev, newItem]);
    setShowPalette(false);
  }, []);

  const handleRemoveWidget = useCallback((id: string) => {
    setItems((prev) => prev.filter((i) => i.id !== id));
  }, []);

  const activeItem = activeId ? items.find((i) => i.id === activeId) : null;
  const activeWidget = activeItem ? getWidget(activeItem.widgetId) : null;

  return (
    <div className="ioc-dashboard">
      {/* Toolbar */}
      <div className="ioc-dashboard__toolbar">
        <h1 className="ioc-dashboard__title">Dashboard</h1>
        <div className="ioc-dashboard__actions">
          <button
            className="ioc-btn ioc-btn--secondary"
            onClick={() => setShowPalette((v) => !v)}
          >
            + Thêm Widget
          </button>
          <button
            className={`ioc-btn ${isEditing ? 'ioc-btn--primary' : 'ioc-btn--outline'}`}
            onClick={() => setIsEditing((v) => !v)}
          >
            {isEditing ? '✓ Lưu Layout' : '✎ Chỉnh sửa'}
          </button>
        </div>
      </div>

      <div className="ioc-dashboard__body">
        {/* Widget Palette */}
        {showPalette && <WidgetPalette onAddWidget={handleAddWidget} />}

        {/* DnD Canvas */}
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <SortableContext items={items.map((i) => i.id)} strategy={rectSortingStrategy}>
            <div className="ioc-dashboard__grid" data-drop-zone="main">
              {items.length === 0 && (
                <div className="ioc-dashboard__empty">
                  <p>Dashboard trống. Nhấn <strong>+ Thêm Widget</strong> để bắt đầu.</p>
                </div>
              )}
              {items.map((item) => {
                const widget = getWidget(item.widgetId);
                if (!widget) return null;
                return (
                  <SortableWidget
                    key={item.id}
                    item={item}
                    widget={widget}
                    isEditing={isEditing}
                    onRemove={handleRemoveWidget}
                  />
                );
              })}
            </div>
          </SortableContext>

          <DragOverlay>
            {activeWidget && activeItem && (
              <div className="ioc-widget ioc-widget--overlay">
                <activeWidget.component config={activeItem.config} />
              </div>
            )}
          </DragOverlay>
        </DndContext>
      </div>
    </div>
  );
}
