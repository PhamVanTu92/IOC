export interface ModuleGql {
  id: string;
  name: string;
  slug: string;
  description?: string;
  icon: string;
  color: string;
  sortOrder: number;
  createdAt: string;
}

export interface ChartGql {
  id: string;
  moduleId: string;
  name: string;
  description?: string;
  chartType: 'line' | 'bar' | 'pie' | 'table' | 'kpi' | 'area' | 'scatter';
  configJson: string;
  sortOrder: number;
  createdAt: string;
}

// JSON shape stored in configJson
export interface ChartConfig {
  title?: string;
  datasource?: string;    // API endpoint or dataset id
  xField?: string;
  yField?: string;
  nameField?: string;     // for pie charts
  valueField?: string;    // for pie/kpi
  aggregation?: 'sum' | 'count' | 'avg' | 'max' | 'min';
  filters?: Record<string, string>;
  colors?: string[];
  unit?: string;          // for KPI
  thresholds?: { warn: number; danger: number };
}

export interface LayoutItem {
  chartId: string;
  x: number;
  y: number;
  w: number;
  h: number;
  visible: boolean;
}

export interface ModulePermissionGql {
  userId: string;
  moduleId: string;
  canView: boolean;
  canEdit: boolean;
  grantedAt: string;
}
