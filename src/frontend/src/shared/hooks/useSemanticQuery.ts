import { useLazyQuery } from '@apollo/client';
import { useCallback, useMemo } from 'react';
import { EXECUTE_QUERY } from '@/graphql/queries';
import type { QueryResultGql, QueryResultParsed, ParsedRow } from '@/graphql/types';
import type { ChartConfig } from '@/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// useSemanticQuery — thực thi QueryInput và trả về kết quả đã parse
// ─────────────────────────────────────────────────────────────────────────────

interface ExecuteQueryData {
  executeQuery: QueryResultGql;
}

interface UseSemanticQueryOptions {
  /** Bỏ qua execute khi config chưa hợp lệ */
  skip?: boolean;
  /** Tự động execute khi config thay đổi */
  autoExecute?: boolean;
}

export interface UseSemanticQueryResult {
  data: QueryResultParsed | null;
  loading: boolean;
  error: string | undefined;
  execute: () => void;
}

export function useSemanticQuery(
  config: ChartConfig,
  options: UseSemanticQueryOptions = {}
): UseSemanticQueryResult {
  const { skip = false } = options;

  const [executeQuery, { data, loading, error }] = useLazyQuery<ExecuteQueryData>(
    EXECUTE_QUERY,
    { fetchPolicy: 'cache-and-network' }
  );

  // Map ChartConfig → QueryRequestInput (khớp với GraphQL schema)
  const queryInput = useMemo(() => buildQueryInput(config), [config]);

  const execute = useCallback(() => {
    if (skip || !config.datasetId) return;
    void executeQuery({ variables: { input: queryInput } });
  }, [executeQuery, queryInput, skip, config.datasetId]);

  // Parse rows từ JSON string → ParsedRow
  const parsed = useMemo<QueryResultParsed | null>(() => {
    if (!data?.executeQuery) return null;
    return parseQueryResult(data.executeQuery);
  }, [data]);

  return {
    data: parsed,
    loading,
    error: error?.message ?? parsed?.metadata.errorMessage,
    execute,
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// buildQueryInput — ChartConfig → GraphQL QueryRequestInput
// ─────────────────────────────────────────────────────────────────────────────

function buildQueryInput(config: ChartConfig) {
  return {
    datasetId: config.datasetId,
    dimensions: config.dimensions.length > 0 ? config.dimensions : undefined,
    measures: config.measures.length > 0 ? config.measures : undefined,
    metrics: config.metrics.length > 0 ? config.metrics : undefined,
    filters: config.filters.length > 0
      ? config.filters.map((f) => ({
          fieldName: f.fieldName,
          operator: f.operator,
          value: f.value,
          values: f.values,
          valueFrom: f.valueFrom,
          valueTo: f.valueTo,
        }))
      : undefined,
    sorts: config.sorts.length > 0
      ? config.sorts.map((s) => ({ fieldName: s.fieldName, direction: s.direction }))
      : undefined,
    limit: config.limit,
    timeDimensionName: config.timeDimensionName,
    granularity: config.granularity,
    timeRange: config.timeRange
      ? {
          preset: config.timeRange.preset,
          from: config.timeRange.from ? new Date(config.timeRange.from).toISOString() : undefined,
          to: config.timeRange.to ? new Date(config.timeRange.to).toISOString() : undefined,
        }
      : undefined,
    forceRefresh: false,
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// parseQueryResult — deserialize JSON string rows
// ─────────────────────────────────────────────────────────────────────────────

function parseQueryResult(result: QueryResultGql): QueryResultParsed {
  const rows: ParsedRow[] = result.rows.map((rowJson) => {
    try {
      return JSON.parse(rowJson) as ParsedRow;
    } catch {
      return {};
    }
  });

  return {
    columns: result.columns,
    rows,
    metadata: result.metadata,
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// Utility: extract typed column values from rows
// ─────────────────────────────────────────────────────────────────────────────

export function getColumnValues(
  rows: ParsedRow[],
  columnName: string
): (string | number | boolean | null)[] {
  return rows.map((row) => row[columnName] ?? null);
}

export function getNumericValues(rows: ParsedRow[], columnName: string): number[] {
  return rows
    .map((row) => row[columnName])
    .filter((v): v is number => typeof v === 'number' || typeof v === 'string')
    .map((v) => Number(v))
    .filter((n) => !isNaN(n));
}

export function getStringValues(rows: ParsedRow[], columnName: string): string[] {
  return rows
    .map((row) => row[columnName])
    .filter((v) => v !== null && v !== undefined)
    .map(String);
}
