import { useReducer, useCallback } from 'react';
import {
  createDefaultConfig,
  type ChartConfig,
  type ChartType,
  type FilterConfig,
  type SortConfig,
  type TimeRangeConfig,
  type ChartVisualOptions,
  type BuilderStep,
} from './types';

// ─────────────────────────────────────────────────────────────────────────────
// useChartConfig — reducer-based state for the chart builder wizard
// ─────────────────────────────────────────────────────────────────────────────

// ── State ─────────────────────────────────────────────────────────────────────

interface ChartBuilderState {
  step: BuilderStep;
  config: ChartConfig;
  isDirty: boolean;
}

// ── Actions ───────────────────────────────────────────────────────────────────

type ChartBuilderAction =
  | { type: 'SET_STEP'; payload: BuilderStep }
  | { type: 'SET_DATASET'; payload: string }
  | { type: 'SET_TITLE'; payload: string }
  | { type: 'SET_CHART_TYPE'; payload: ChartType }
  | { type: 'SET_DIMENSIONS'; payload: string[] }
  | { type: 'TOGGLE_DIMENSION'; payload: string }
  | { type: 'SET_MEASURES'; payload: string[] }
  | { type: 'TOGGLE_MEASURE'; payload: string }
  | { type: 'SET_METRICS'; payload: string[] }
  | { type: 'TOGGLE_METRIC'; payload: string }
  | { type: 'SET_FILTERS'; payload: FilterConfig[] }
  | { type: 'ADD_FILTER'; payload: FilterConfig }
  | { type: 'REMOVE_FILTER'; payload: number }
  | { type: 'SET_SORTS'; payload: SortConfig[] }
  | { type: 'TOGGLE_SORT'; payload: SortConfig }
  | { type: 'SET_LIMIT'; payload: number }
  | { type: 'SET_TIME_DIMENSION'; payload: string | undefined }
  | { type: 'SET_GRANULARITY'; payload: ChartConfig['granularity'] }
  | { type: 'SET_TIME_RANGE'; payload: TimeRangeConfig | undefined }
  | { type: 'SET_VISUAL_OPTIONS'; payload: Partial<ChartVisualOptions> }
  | { type: 'RESET'; payload?: Partial<ChartConfig> }
  | { type: 'LOAD_CONFIG'; payload: ChartConfig };

// ── Reducer ───────────────────────────────────────────────────────────────────

function reducer(state: ChartBuilderState, action: ChartBuilderAction): ChartBuilderState {
  switch (action.type) {
    case 'SET_STEP':
      return { ...state, step: action.payload };

    case 'SET_DATASET':
      // Resetting fields when dataset changes
      return {
        ...state,
        isDirty: true,
        config: {
          ...state.config,
          datasetId: action.payload,
          dimensions: [],
          measures: [],
          metrics: [],
          filters: [],
          sorts: [],
          timeDimensionName: undefined,
          granularity: undefined,
          timeRange: undefined,
        },
      };

    case 'SET_TITLE':
      return { ...state, isDirty: true, config: { ...state.config, title: action.payload } };

    case 'SET_CHART_TYPE':
      return { ...state, isDirty: true, config: { ...state.config, chartType: action.payload } };

    case 'SET_DIMENSIONS':
      return { ...state, isDirty: true, config: { ...state.config, dimensions: action.payload } };

    case 'TOGGLE_DIMENSION': {
      const dims = state.config.dimensions;
      const next = dims.includes(action.payload)
        ? dims.filter((d) => d !== action.payload)
        : [...dims, action.payload];
      return { ...state, isDirty: true, config: { ...state.config, dimensions: next } };
    }

    case 'SET_MEASURES':
      return { ...state, isDirty: true, config: { ...state.config, measures: action.payload } };

    case 'TOGGLE_MEASURE': {
      const measures = state.config.measures;
      const next = measures.includes(action.payload)
        ? measures.filter((m) => m !== action.payload)
        : [...measures, action.payload];
      return { ...state, isDirty: true, config: { ...state.config, measures: next } };
    }

    case 'SET_METRICS':
      return { ...state, isDirty: true, config: { ...state.config, metrics: action.payload } };

    case 'TOGGLE_METRIC': {
      const metrics = state.config.metrics;
      const next = metrics.includes(action.payload)
        ? metrics.filter((m) => m !== action.payload)
        : [...metrics, action.payload];
      return { ...state, isDirty: true, config: { ...state.config, metrics: next } };
    }

    case 'SET_FILTERS':
      return { ...state, isDirty: true, config: { ...state.config, filters: action.payload } };

    case 'ADD_FILTER':
      return {
        ...state,
        isDirty: true,
        config: { ...state.config, filters: [...state.config.filters, action.payload] },
      };

    case 'REMOVE_FILTER':
      return {
        ...state,
        isDirty: true,
        config: {
          ...state.config,
          filters: state.config.filters.filter((_, i) => i !== action.payload),
        },
      };

    case 'SET_SORTS':
      return { ...state, isDirty: true, config: { ...state.config, sorts: action.payload } };

    case 'TOGGLE_SORT': {
      const { fieldName, direction } = action.payload;
      const sorts = state.config.sorts;
      const existing = sorts.find((s) => s.fieldName === fieldName);
      let next: SortConfig[];
      if (!existing) {
        next = [...sorts, { fieldName, direction }];
      } else if (existing.direction !== direction) {
        next = sorts.map((s) => (s.fieldName === fieldName ? { ...s, direction } : s));
      } else {
        next = sorts.filter((s) => s.fieldName !== fieldName);
      }
      return { ...state, isDirty: true, config: { ...state.config, sorts: next } };
    }

    case 'SET_LIMIT':
      return { ...state, isDirty: true, config: { ...state.config, limit: action.payload } };

    case 'SET_TIME_DIMENSION':
      return {
        ...state,
        isDirty: true,
        config: { ...state.config, timeDimensionName: action.payload },
      };

    case 'SET_GRANULARITY':
      return { ...state, isDirty: true, config: { ...state.config, granularity: action.payload } };

    case 'SET_TIME_RANGE':
      return { ...state, isDirty: true, config: { ...state.config, timeRange: action.payload } };

    case 'SET_VISUAL_OPTIONS':
      return {
        ...state,
        isDirty: true,
        config: {
          ...state.config,
          visualOptions: { ...state.config.visualOptions, ...action.payload },
        },
      };

    case 'RESET':
      return {
        step: 'dataset',
        config: createDefaultConfig(action.payload),
        isDirty: false,
      };

    case 'LOAD_CONFIG':
      return { step: 'preview', config: action.payload, isDirty: false };

    default:
      return state;
  }
}

// ── Hook ──────────────────────────────────────────────────────────────────────

export interface UseChartConfigReturn {
  state: ChartBuilderState;
  // Step navigation
  goToStep: (step: BuilderStep) => void;
  nextStep: () => void;
  prevStep: () => void;
  // Config mutations
  setDataset: (id: string) => void;
  setTitle: (title: string) => void;
  setChartType: (type: ChartType) => void;
  toggleDimension: (name: string) => void;
  toggleMeasure: (name: string) => void;
  toggleMetric: (name: string) => void;
  setFilters: (filters: FilterConfig[]) => void;
  addFilter: (filter: FilterConfig) => void;
  removeFilter: (index: number) => void;
  toggleSort: (sort: SortConfig) => void;
  setLimit: (limit: number) => void;
  setTimeDimension: (name: string | undefined) => void;
  setGranularity: (g: ChartConfig['granularity']) => void;
  setTimeRange: (range: TimeRangeConfig | undefined) => void;
  setVisualOptions: (opts: Partial<ChartVisualOptions>) => void;
  reset: (overrides?: Partial<ChartConfig>) => void;
  loadConfig: (config: ChartConfig) => void;
}

const STEP_ORDER: BuilderStep[] = ['dataset', 'fields', 'chartType', 'preview'];

export function useChartConfig(initial?: Partial<ChartConfig>): UseChartConfigReturn {
  const [state, dispatch] = useReducer(reducer, {
    step: 'dataset',
    config: createDefaultConfig(initial),
    isDirty: false,
  });

  const goToStep = useCallback((step: BuilderStep) => dispatch({ type: 'SET_STEP', payload: step }), []);
  const nextStep = useCallback(() => {
    const idx = STEP_ORDER.indexOf(state.step);
    if (idx < STEP_ORDER.length - 1) dispatch({ type: 'SET_STEP', payload: STEP_ORDER[idx + 1] });
  }, [state.step]);
  const prevStep = useCallback(() => {
    const idx = STEP_ORDER.indexOf(state.step);
    if (idx > 0) dispatch({ type: 'SET_STEP', payload: STEP_ORDER[idx - 1] });
  }, [state.step]);

  const setDataset = useCallback((id: string) => dispatch({ type: 'SET_DATASET', payload: id }), []);
  const setTitle = useCallback((title: string) => dispatch({ type: 'SET_TITLE', payload: title }), []);
  const setChartType = useCallback((type: ChartType) => dispatch({ type: 'SET_CHART_TYPE', payload: type }), []);
  const toggleDimension = useCallback((name: string) => dispatch({ type: 'TOGGLE_DIMENSION', payload: name }), []);
  const toggleMeasure = useCallback((name: string) => dispatch({ type: 'TOGGLE_MEASURE', payload: name }), []);
  const toggleMetric = useCallback((name: string) => dispatch({ type: 'TOGGLE_METRIC', payload: name }), []);
  const setFilters = useCallback((filters: FilterConfig[]) => dispatch({ type: 'SET_FILTERS', payload: filters }), []);
  const addFilter = useCallback((filter: FilterConfig) => dispatch({ type: 'ADD_FILTER', payload: filter }), []);
  const removeFilter = useCallback((index: number) => dispatch({ type: 'REMOVE_FILTER', payload: index }), []);
  const toggleSort = useCallback((sort: SortConfig) => dispatch({ type: 'TOGGLE_SORT', payload: sort }), []);
  const setLimit = useCallback((limit: number) => dispatch({ type: 'SET_LIMIT', payload: limit }), []);
  const setTimeDimension = useCallback((name: string | undefined) => dispatch({ type: 'SET_TIME_DIMENSION', payload: name }), []);
  const setGranularity = useCallback((g: ChartConfig['granularity']) => dispatch({ type: 'SET_GRANULARITY', payload: g }), []);
  const setTimeRange = useCallback((range: TimeRangeConfig | undefined) => dispatch({ type: 'SET_TIME_RANGE', payload: range }), []);
  const setVisualOptions = useCallback((opts: Partial<ChartVisualOptions>) => dispatch({ type: 'SET_VISUAL_OPTIONS', payload: opts }), []);
  const reset = useCallback((overrides?: Partial<ChartConfig>) => dispatch({ type: 'RESET', payload: overrides }), []);
  const loadConfig = useCallback((config: ChartConfig) => dispatch({ type: 'LOAD_CONFIG', payload: config }), []);

  return {
    state,
    goToStep,
    nextStep,
    prevStep,
    setDataset,
    setTitle,
    setChartType,
    toggleDimension,
    toggleMeasure,
    toggleMetric,
    setFilters,
    addFilter,
    removeFilter,
    toggleSort,
    setLimit,
    setTimeDimension,
    setGranularity,
    setTimeRange,
    setVisualOptions,
    reset,
    loadConfig,
  };
}
