// ─────────────────────────────────────────────────────────────────────────────
// GraphQL response types — khớp 1-1 với HotChocolate schema ở backend
// ─────────────────────────────────────────────────────────────────────────────

// ── Metadata types ────────────────────────────────────────────────────────────

export interface DimensionGql {
  id: string;
  datasetId: string;
  name: string;
  displayName: string;
  description?: string;
  columnName: string;
  customSqlExpression?: string;
  dataType: string;
  format?: string;
  isTimeDimension: boolean;
  defaultGranularity?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface MeasureGql {
  id: string;
  datasetId: string;
  name: string;
  displayName: string;
  description?: string;
  columnName: string;
  customSqlExpression?: string;
  aggregationType: string;
  dataType: string;
  format?: string;
  filterExpression?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface MetricGql {
  id: string;
  datasetId: string;
  name: string;
  displayName: string;
  description?: string;
  expression: string;
  dataType: string;
  format?: string;
  dependsOnMeasures: string[];
  sortOrder: number;
  isActive: boolean;
}

export interface DatasetGql {
  id: string;
  tenantId: string;
  name: string;
  description?: string;
  sourceType: string;
  schemaName?: string;
  tableName?: string;
  customSql?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  dimensions: DimensionGql[];
  measures: MeasureGql[];
  metrics: MetricGql[];
}

// ── Query Result types ─────────────────────────────────────────────────────────

export interface QueryResultColumnGql {
  name: string;
  displayName: string;
  dataType: string;
  format?: string;
  fieldType: 'dimension' | 'measure' | 'metric';
}

export interface QueryExecutionMetadataGql {
  generatedSql?: string;
  executionTimeMs: number;
  totalRows: number;
  fromCache: boolean;
  cacheKey?: string;
  executedAt: string;
  errorMessage?: string;
}

export interface QueryResultGql {
  columns: QueryResultColumnGql[];
  /** Mỗi row là JSON string — phải parse ở client */
  rows: string[];
  metadata: QueryExecutionMetadataGql;
}

// ── Dashboard GQL types ───────────────────────────────────────────────────────

export interface DashboardGql {
  id: string;
  tenantId: string;
  createdBy: string;
  title: string;
  description?: string;
  /** Full serialized frontend DashboardConfig JSON string */
  configJson: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface DashboardSummaryGql {
  id: string;
  title: string;
  description?: string;
  isActive: boolean;
  updatedAt: string;
  widgetCount: number;
}

// ── Parsed row type ────────────────────────────────────────────────────────────

/** Row sau khi JSON.parse — map từ column name → giá trị */
export type ParsedRow = Record<string, string | number | boolean | null>;

/** QueryResult sau khi parse rows */
export interface QueryResultParsed {
  columns: QueryResultColumnGql[];
  rows: ParsedRow[];
  metadata: QueryExecutionMetadataGql;
}
