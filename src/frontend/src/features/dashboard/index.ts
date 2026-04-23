// ─────────────────────────────────────────────────────────────────────────────
// dashboard — public API
// ─────────────────────────────────────────────────────────────────────────────

export { DashboardPage } from './DashboardPage';
export { DashboardListPage } from './DashboardListPage';
export { DashboardGrid } from './DashboardGrid';
export { DashboardToolbar } from './DashboardToolbar';
export { WidgetLibrary } from './WidgetLibrary';
export { ChartBuilderModal } from './ChartBuilderModal';
export { DashboardWidgetShell, DashboardWidgetGhost } from './DashboardWidgetShell';
export { useDashboardStore, selectWidgets, selectEditMode, selectIsDirty, selectDashboard } from './useDashboardStore';
export {
  useDashboardList,
  useDashboardLoad,
  useDashboardSave,
  listLocalDashboards,
} from './useDashboardPersistence';
export type { UseDashboardListResult, UseDashboardLoadResult, UseDashboardSaveResult } from './useDashboardPersistence';

export {
  createDefaultDashboard,
  createWidget,
  widgetHeightPx,
  gridColumnSpan,
  ROW_HEIGHT_PX,
  WIDGET_SIZE_PRESETS,
} from './types';

export type {
  DashboardConfig,
  DashboardWidget,
  WidgetLayout,
  WidgetSizePreset,
} from './types';
