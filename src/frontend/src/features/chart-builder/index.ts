// ─────────────────────────────────────────────────────────────────────────────
// chart-builder — public API
// ─────────────────────────────────────────────────────────────────────────────

export { ChartBuilder } from './ChartBuilder';
export { ChartPreview } from './ChartPreview';
export { ChartTypePicker } from './ChartTypePicker';
export { DatasetSelector } from './DatasetSelector';
export { FieldPicker } from './FieldPicker';
export { useChartConfig } from './useChartConfig';
export type { UseChartConfigReturn } from './useChartConfig';

export {
  createDefaultConfig,
  isConfigValid,
  CHART_TYPE_META,
} from './types';

export type {
  ChartConfig,
  ChartType,
  ChartVisualOptions,
  FilterConfig,
  SortConfig,
  TimeRangeConfig,
  BuilderStep,
  Granularity,
  TimePreset,
  FilterOperator,
  ChartTypeMeta,
} from './types';
