// ─────────────────────────────────────────────────────────────────────────────
// Chart Builder — core types
// Serializable → có thể lưu vào DB / LocalStorage / copy-paste
// ─────────────────────────────────────────────────────────────────────────────
import { generateUUID } from '@/shared/utils/uuid';

export type ChartType =
  | 'line'
  | 'bar'
  | 'bar_horizontal'
  | 'area'
  | 'pie'
  | 'donut'
  | 'scatter'
  | 'table'
  | 'kpi'
  | 'heatmap';

export type Granularity = 'hour' | 'day' | 'week' | 'month' | 'quarter' | 'year';

export type TimePreset =
  | 'today' | 'yesterday'
  | 'last7days' | 'last14days' | 'last30days' | 'last90days'
  | 'thisWeek' | 'thisMonth' | 'lastMonth'
  | 'thisQuarter' | 'thisYear' | 'lastYear';

export interface TimeRangeConfig {
  preset?: TimePreset;
  from?: string;   // ISO date string
  to?: string;
}

export type FilterOperator =
  | 'Equals' | 'NotEquals'
  | 'GreaterThan' | 'GreaterThanOrEquals'
  | 'LessThan' | 'LessThanOrEquals'
  | 'In' | 'NotIn'
  | 'Contains' | 'NotContains'
  | 'StartsWith' | 'EndsWith'
  | 'Between'
  | 'IsNull' | 'IsNotNull';

export interface FilterConfig {
  fieldName: string;
  operator: FilterOperator;
  value?: string;
  values?: string[];
  valueFrom?: string;
  valueTo?: string;
}

export interface SortConfig {
  fieldName: string;
  direction: 'ASC' | 'DESC';
}

// ── ChartConfig — fully serializable chart definition ─────────────────────────

export interface ChartConfig {
  /** UUID — tự sinh nếu là mới */
  id: string;
  title: string;
  chartType: ChartType;
  datasetId: string;

  /** Tên dimensions cần SELECT + GROUP BY */
  dimensions: string[];
  /** Tên measures cần aggregate */
  measures: string[];
  /** Tên metrics (computed expressions) */
  metrics: string[];

  filters: FilterConfig[];
  sorts: SortConfig[];

  /** Time dimension name (để filter theo thời gian) */
  timeDimensionName?: string;
  granularity?: Granularity;
  timeRange?: TimeRangeConfig;

  limit: number;

  /** Chart-specific visual options (màu, label, legend...) */
  visualOptions?: ChartVisualOptions;
}

export interface ChartVisualOptions {
  showLegend?: boolean;
  showDataLabels?: boolean;
  smooth?: boolean;          // line chart
  stacked?: boolean;         // bar/area chart
  colorPalette?: string[];
  xAxisLabel?: string;
  yAxisLabel?: string;
  /** KPI chart */
  prefix?: string;
  suffix?: string;
  comparisonField?: string;
}

// ── Chart builder step state ───────────────────────────────────────────────────

export type BuilderStep = 'dataset' | 'fields' | 'chartType' | 'preview';

export interface ChartBuilderState {
  step: BuilderStep;
  config: ChartConfig;
  isDirty: boolean;
}

// ── Helper: default config factory ────────────────────────────────────────────

export function createDefaultConfig(overrides?: Partial<ChartConfig>): ChartConfig {
  return {
    id: generateUUID(),
    title: 'Chart mới',
    chartType: 'bar',
    datasetId: '',
    dimensions: [],
    measures: [],
    metrics: [],
    filters: [],
    sorts: [],
    limit: 1000,
    ...overrides,
  };
}

// ── Chart type metadata (dùng cho UI picker) ──────────────────────────────────

export interface ChartTypeMeta {
  type: ChartType;
  label: string;
  icon: string;          // emoji hoặc icon name
  description: string;
  minDimensions: number;
  minMeasures: number;
  supportsTimeDimension: boolean;
}

export const CHART_TYPE_META: ChartTypeMeta[] = [
  {
    type: 'bar',
    label: 'Bar Chart',
    icon: '📊',
    description: 'So sánh giá trị giữa các nhóm',
    minDimensions: 1,
    minMeasures: 1,
    supportsTimeDimension: true,
  },
  {
    type: 'line',
    label: 'Line Chart',
    icon: '📈',
    description: 'Xu hướng theo thời gian',
    minDimensions: 1,
    minMeasures: 1,
    supportsTimeDimension: true,
  },
  {
    type: 'area',
    label: 'Area Chart',
    icon: '📉',
    description: 'Xu hướng với diện tích tô màu',
    minDimensions: 1,
    minMeasures: 1,
    supportsTimeDimension: true,
  },
  {
    type: 'pie',
    label: 'Pie Chart',
    icon: '🥧',
    description: 'Tỷ lệ phần trăm giữa các phần',
    minDimensions: 1,
    minMeasures: 1,
    supportsTimeDimension: false,
  },
  {
    type: 'donut',
    label: 'Donut Chart',
    icon: '🍩',
    description: 'Tỷ lệ với vòng tròn giữa rỗng',
    minDimensions: 1,
    minMeasures: 1,
    supportsTimeDimension: false,
  },
  {
    type: 'scatter',
    label: 'Scatter Plot',
    icon: '✨',
    description: 'Tương quan giữa 2 measure',
    minDimensions: 0,
    minMeasures: 2,
    supportsTimeDimension: false,
  },
  {
    type: 'table',
    label: 'Table',
    icon: '📋',
    description: 'Dữ liệu dạng bảng có thể sort',
    minDimensions: 0,
    minMeasures: 0,
    supportsTimeDimension: true,
  },
  {
    type: 'kpi',
    label: 'KPI Card',
    icon: '🎯',
    description: 'Hiển thị một chỉ số quan trọng',
    minDimensions: 0,
    minMeasures: 1,
    supportsTimeDimension: false,
  },
];

/** Kiểm tra config có đủ điều kiện để render chart không */
export function isConfigValid(config: ChartConfig): boolean {
  if (!config.datasetId) return false;
  const meta = CHART_TYPE_META.find((m) => m.type === config.chartType);
  if (!meta) return false;
  const totalFields = config.dimensions.length + config.measures.length + config.metrics.length;
  if (totalFields === 0) return false;
  if (config.dimensions.length < meta.minDimensions) return false;
  if (config.measures.length < meta.minMeasures) return false;
  return true;
}
