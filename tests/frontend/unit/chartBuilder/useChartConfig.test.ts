import { renderHook, act } from '@testing-library/react';
import { useChartConfig } from '../../../../src/frontend/src/features/chart-builder/useChartConfig';

// ─────────────────────────────────────────────────────────────────────────────
// useChartConfig — reducer-based state management for chart builder
// ─────────────────────────────────────────────────────────────────────────────

describe('useChartConfig — initial state', () => {
  it('starts on dataset step', () => {
    const { result } = renderHook(() => useChartConfig());
    expect(result.current.state.step).toBe('dataset');
  });

  it('starts with empty datasetId', () => {
    const { result } = renderHook(() => useChartConfig());
    expect(result.current.state.config.datasetId).toBe('');
  });

  it('starts with isDirty = false', () => {
    const { result } = renderHook(() => useChartConfig());
    expect(result.current.state.isDirty).toBe(false);
  });

  it('applies initial overrides', () => {
    const { result } = renderHook(() =>
      useChartConfig({ datasetId: 'ds-42', chartType: 'pie' })
    );
    expect(result.current.state.config.datasetId).toBe('ds-42');
    expect(result.current.state.config.chartType).toBe('pie');
  });
});

describe('useChartConfig — step navigation', () => {
  it('nextStep advances step', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.nextStep());
    expect(result.current.state.step).toBe('fields');
  });

  it('nextStep does not go past preview', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => {
      result.current.nextStep(); // fields
      result.current.nextStep(); // chartType
      result.current.nextStep(); // preview
      result.current.nextStep(); // stays preview
    });
    expect(result.current.state.step).toBe('preview');
  });

  it('prevStep goes back', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.nextStep());
    act(() => result.current.prevStep());
    expect(result.current.state.step).toBe('dataset');
  });

  it('prevStep does not go before dataset', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.prevStep());
    expect(result.current.state.step).toBe('dataset');
  });

  it('goToStep jumps directly', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.goToStep('preview'));
    expect(result.current.state.step).toBe('preview');
  });
});

describe('useChartConfig — config mutations', () => {
  it('setDataset updates datasetId and clears fields', () => {
    const { result } = renderHook(() =>
      useChartConfig({ dimensions: ['city'], measures: ['revenue'] })
    );
    act(() => result.current.setDataset('new-ds'));
    const cfg = result.current.state.config;
    expect(cfg.datasetId).toBe('new-ds');
    expect(cfg.dimensions).toEqual([]);
    expect(cfg.measures).toEqual([]);
    expect(result.current.state.isDirty).toBe(true);
  });

  it('setTitle updates title', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.setTitle('My Chart'));
    expect(result.current.state.config.title).toBe('My Chart');
    expect(result.current.state.isDirty).toBe(true);
  });

  it('setChartType updates chartType', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.setChartType('line'));
    expect(result.current.state.config.chartType).toBe('line');
  });

  it('toggleDimension adds then removes dimension', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.toggleDimension('category'));
    expect(result.current.state.config.dimensions).toContain('category');
    act(() => result.current.toggleDimension('category'));
    expect(result.current.state.config.dimensions).not.toContain('category');
  });

  it('toggleMeasure works correctly', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.toggleMeasure('revenue'));
    act(() => result.current.toggleMeasure('profit'));
    expect(result.current.state.config.measures).toEqual(['revenue', 'profit']);
    act(() => result.current.toggleMeasure('revenue'));
    expect(result.current.state.config.measures).toEqual(['profit']);
  });

  it('toggleMetric works correctly', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.toggleMetric('conversion'));
    expect(result.current.state.config.metrics).toContain('conversion');
  });

  it('addFilter appends a filter', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() =>
      result.current.addFilter({ fieldName: 'status', operator: 'Equals', value: 'active' })
    );
    expect(result.current.state.config.filters).toHaveLength(1);
    expect(result.current.state.config.filters[0].fieldName).toBe('status');
  });

  it('removeFilter removes by index', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => {
      result.current.addFilter({ fieldName: 'a', operator: 'Equals', value: '1' });
      result.current.addFilter({ fieldName: 'b', operator: 'Equals', value: '2' });
    });
    act(() => result.current.removeFilter(0));
    expect(result.current.state.config.filters).toHaveLength(1);
    expect(result.current.state.config.filters[0].fieldName).toBe('b');
  });

  it('toggleSort adds a sort, then changes direction, then removes', () => {
    const { result } = renderHook(() => useChartConfig());
    // Add ASC
    act(() => result.current.toggleSort({ fieldName: 'revenue', direction: 'ASC' }));
    expect(result.current.state.config.sorts).toEqual([{ fieldName: 'revenue', direction: 'ASC' }]);
    // Change to DESC
    act(() => result.current.toggleSort({ fieldName: 'revenue', direction: 'DESC' }));
    expect(result.current.state.config.sorts).toEqual([{ fieldName: 'revenue', direction: 'DESC' }]);
    // Remove (same direction toggle)
    act(() => result.current.toggleSort({ fieldName: 'revenue', direction: 'DESC' }));
    expect(result.current.state.config.sorts).toEqual([]);
  });

  it('setLimit updates limit', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.setLimit(5000));
    expect(result.current.state.config.limit).toBe(5000);
  });

  it('setTimeDimension and setGranularity', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.setTimeDimension('created_at'));
    act(() => result.current.setGranularity('month'));
    expect(result.current.state.config.timeDimensionName).toBe('created_at');
    expect(result.current.state.config.granularity).toBe('month');
  });

  it('setTimeRange sets the range', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.setTimeRange({ preset: 'last30days' }));
    expect(result.current.state.config.timeRange?.preset).toBe('last30days');
  });

  it('setVisualOptions merges options', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.setVisualOptions({ showLegend: true, smooth: false }));
    act(() => result.current.setVisualOptions({ stacked: true }));
    const vo = result.current.state.config.visualOptions;
    expect(vo?.showLegend).toBe(true);
    expect(vo?.smooth).toBe(false);
    expect(vo?.stacked).toBe(true);
  });
});

describe('useChartConfig — reset and load', () => {
  it('reset clears state and marks isDirty = false', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.setTitle('Old Title'));
    act(() => result.current.toggleMeasure('revenue'));
    act(() => result.current.reset());
    expect(result.current.state.isDirty).toBe(false);
    expect(result.current.state.config.title).toBe('Chart mới');
    expect(result.current.state.config.measures).toEqual([]);
    expect(result.current.state.step).toBe('dataset');
  });

  it('reset with overrides keeps overrides', () => {
    const { result } = renderHook(() => useChartConfig());
    act(() => result.current.reset({ datasetId: 'ds-reset' }));
    expect(result.current.state.config.datasetId).toBe('ds-reset');
  });

  it('loadConfig sets config and jumps to preview', () => {
    const { result } = renderHook(() => useChartConfig());
    const loaded = {
      id: 'loaded-id',
      title: 'Loaded Chart',
      chartType: 'line' as const,
      datasetId: 'ds-loaded',
      dimensions: ['month'],
      measures: ['revenue'],
      metrics: [],
      filters: [],
      sorts: [],
      limit: 500,
    };
    act(() => result.current.loadConfig(loaded));
    expect(result.current.state.config).toEqual(loaded);
    expect(result.current.state.step).toBe('preview');
    expect(result.current.state.isDirty).toBe(false);
  });
});
