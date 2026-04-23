import type { ChartConfig } from '@/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// Dashboard — core types
// Fully serializable → stored in DB / LocalStorage / copy-paste
// ─────────────────────────────────────────────────────────────────────────────

/** Grid coordinates in a 12-column layout */
export interface WidgetLayout {
  /** Column start (0-based, 0–11) */
  x: number;
  /** Row start (0-based) */
  y: number;
  /** Column span (1–12) */
  w: number;
  /** Row span (1–N); 1 unit ≈ ROW_HEIGHT_PX px */
  h: number;
}

/** Size presets for quick resize */
export type WidgetSizePreset = 'small' | 'medium' | 'large' | 'wide' | 'full';

export const WIDGET_SIZE_PRESETS: Record<WidgetSizePreset, Pick<WidgetLayout, 'w' | 'h'>> = {
  small:  { w: 3, h: 2 },   // quarter-width, short
  medium: { w: 6, h: 3 },   // half-width, standard
  large:  { w: 6, h: 4 },   // half-width, tall
  wide:   { w: 9, h: 3 },   // three-quarter-width
  full:   { w: 12, h: 4 },  // full-width
};

/** Pixel height of one row unit */
export const ROW_HEIGHT_PX = 120;

/** A widget placed on the dashboard */
export interface DashboardWidget {
  /** Unique id for the widget instance (not the chart config id) */
  id: string;
  /** The chart definition — this is what gets rendered */
  chartConfig: ChartConfig;
  /** Grid position + size */
  layout: WidgetLayout;
}

/** Full dashboard definition */
export interface DashboardConfig {
  /** UUID */
  id: string;
  title: string;
  description?: string;
  /** Ordered list of widgets (order determines z-index / display order in list) */
  widgets: DashboardWidget[];
  /** ISO strings */
  createdAt: string;
  updatedAt: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

export function createDefaultDashboard(overrides?: Partial<DashboardConfig>): DashboardConfig {
  const now = new Date().toISOString();
  return {
    id: crypto.randomUUID(),
    title: 'Dashboard mới',
    description: '',
    widgets: [],
    createdAt: now,
    updatedAt: now,
    ...overrides,
  };
}

export function createWidget(
  chartConfig: ChartConfig,
  layout?: Partial<WidgetLayout>
): DashboardWidget {
  return {
    id: crypto.randomUUID(),
    chartConfig,
    layout: {
      x: 0,
      y: 0,
      w: WIDGET_SIZE_PRESETS.medium.w,
      h: WIDGET_SIZE_PRESETS.medium.h,
      ...layout,
    },
  };
}

/** Compute widget height in pixels from layout */
export function widgetHeightPx(h: number): number {
  return h * ROW_HEIGHT_PX;
}

/** How many grid columns does a widget span as a CSS grid-column value */
export function gridColumnSpan(w: number): string {
  return `span ${w}`;
}
