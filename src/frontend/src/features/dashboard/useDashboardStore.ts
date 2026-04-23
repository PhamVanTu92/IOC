import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import type { ChartConfig } from '@/features/chart-builder/types';
import {
  createDefaultDashboard,
  createWidget,
  WIDGET_SIZE_PRESETS,
  type DashboardConfig,
  type DashboardWidget,
  type WidgetLayout,
  type WidgetSizePreset,
} from './types';

// ─────────────────────────────────────────────────────────────────────────────
// Dashboard Zustand store with Immer for ergonomic mutations
// ─────────────────────────────────────────────────────────────────────────────

interface DashboardState {
  /** Current dashboard being edited / viewed */
  dashboard: DashboardConfig;
  /** Whether the user has unsaved changes */
  isDirty: boolean;
  /** Edit mode → drag handles / resize / delete visible */
  editMode: boolean;
  /** Which widget id is being edited in the ChartBuilder modal */
  editingWidgetId: string | null;

  // ── Lifecycle ───────────────────────────────────────────────────────────────
  loadDashboard: (config: DashboardConfig) => void;
  resetDashboard: (overrides?: Partial<DashboardConfig>) => void;

  // ── Dashboard metadata ──────────────────────────────────────────────────────
  setTitle: (title: string) => void;
  setDescription: (desc: string) => void;

  // ── Edit mode ───────────────────────────────────────────────────────────────
  setEditMode: (on: boolean) => void;
  toggleEditMode: () => void;

  // ── Widget CRUD ─────────────────────────────────────────────────────────────
  addWidget: (chartConfig: ChartConfig, layout?: Partial<WidgetLayout>) => void;
  removeWidget: (widgetId: string) => void;
  updateWidgetConfig: (widgetId: string, chartConfig: ChartConfig) => void;
  updateWidgetLayout: (widgetId: string, layout: Partial<WidgetLayout>) => void;
  resizeWidget: (widgetId: string, preset: WidgetSizePreset) => void;
  reorderWidgets: (orderedIds: string[]) => void;

  // ── Modal ───────────────────────────────────────────────────────────────────
  openEditor: (widgetId: string) => void;
  closeEditor: () => void;

  // ── Dirty flag management ───────────────────────────────────────────────────
  markSaved: () => void;
}

export const useDashboardStore = create<DashboardState>()(
  immer((set) => ({
    dashboard: createDefaultDashboard(),
    isDirty: false,
    editMode: false,
    editingWidgetId: null,

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    loadDashboard: (config) =>
      set((s) => {
        s.dashboard = config;
        s.isDirty = false;
        s.editMode = false;
        s.editingWidgetId = null;
      }),

    resetDashboard: (overrides) =>
      set((s) => {
        s.dashboard = createDefaultDashboard(overrides);
        s.isDirty = false;
        s.editMode = true; // Start in edit mode for new dashboards
        s.editingWidgetId = null;
      }),

    // ── Metadata ──────────────────────────────────────────────────────────────

    setTitle: (title) =>
      set((s) => {
        s.dashboard.title = title;
        s.dashboard.updatedAt = new Date().toISOString();
        s.isDirty = true;
      }),

    setDescription: (desc) =>
      set((s) => {
        s.dashboard.description = desc;
        s.dashboard.updatedAt = new Date().toISOString();
        s.isDirty = true;
      }),

    // ── Edit mode ─────────────────────────────────────────────────────────────

    setEditMode: (on) => set((s) => { s.editMode = on; }),
    toggleEditMode: () => set((s) => { s.editMode = !s.editMode; }),

    // ── Widget CRUD ───────────────────────────────────────────────────────────

    addWidget: (chartConfig, layout) =>
      set((s) => {
        const widget = createWidget(chartConfig, layout);
        // Place new widget after the last widget in y order
        const maxY = s.dashboard.widgets.reduce(
          (acc, w) => Math.max(acc, w.layout.y + w.layout.h),
          0
        );
        widget.layout.y = maxY;
        s.dashboard.widgets.push(widget);
        s.dashboard.updatedAt = new Date().toISOString();
        s.isDirty = true;
      }),

    removeWidget: (widgetId) =>
      set((s) => {
        s.dashboard.widgets = s.dashboard.widgets.filter((w) => w.id !== widgetId);
        s.dashboard.updatedAt = new Date().toISOString();
        s.isDirty = true;
      }),

    updateWidgetConfig: (widgetId, chartConfig) =>
      set((s) => {
        const w = s.dashboard.widgets.find((w) => w.id === widgetId);
        if (w) {
          w.chartConfig = chartConfig;
          s.dashboard.updatedAt = new Date().toISOString();
          s.isDirty = true;
        }
      }),

    updateWidgetLayout: (widgetId, layout) =>
      set((s) => {
        const w = s.dashboard.widgets.find((w) => w.id === widgetId);
        if (w) {
          Object.assign(w.layout, layout);
          s.dashboard.updatedAt = new Date().toISOString();
          s.isDirty = true;
        }
      }),

    resizeWidget: (widgetId, preset) =>
      set((s) => {
        const w = s.dashboard.widgets.find((w) => w.id === widgetId);
        if (w) {
          const size = WIDGET_SIZE_PRESETS[preset];
          w.layout.w = size.w;
          w.layout.h = size.h;
          s.dashboard.updatedAt = new Date().toISOString();
          s.isDirty = true;
        }
      }),

    reorderWidgets: (orderedIds) =>
      set((s) => {
        const map = new Map<string, DashboardWidget>(
          s.dashboard.widgets.map((w) => [w.id, w])
        );
        s.dashboard.widgets = orderedIds
          .map((id) => map.get(id))
          .filter((w): w is DashboardWidget => w !== undefined);
        s.dashboard.updatedAt = new Date().toISOString();
        s.isDirty = true;
      }),

    // ── Modal ─────────────────────────────────────────────────────────────────

    openEditor: (widgetId) => set((s) => { s.editingWidgetId = widgetId; }),
    closeEditor: () => set((s) => { s.editingWidgetId = null; }),

    // ── Dirty flag ────────────────────────────────────────────────────────────

    markSaved: () =>
      set((s) => {
        s.isDirty = false;
        s.dashboard.updatedAt = new Date().toISOString();
      }),
  }))
);

// ─────────────────────────────────────────────────────────────────────────────
// Selectors (memoized via shallow equality in caller)
// ─────────────────────────────────────────────────────────────────────────────

export const selectWidgets = (s: DashboardState) => s.dashboard.widgets;
export const selectEditMode = (s: DashboardState) => s.editMode;
export const selectIsDirty = (s: DashboardState) => s.isDirty;
export const selectEditingWidgetId = (s: DashboardState) => s.editingWidgetId;
export const selectDashboard = (s: DashboardState) => s.dashboard;
