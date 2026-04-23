import {
  createDefaultConfig,
  isConfigValid,
  CHART_TYPE_META,
  type ChartConfig,
} from '../../../../src/frontend/src/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// chart-builder/types.ts — createDefaultConfig + isConfigValid
// ─────────────────────────────────────────────────────────────────────────────

describe('createDefaultConfig', () => {
  it('generates a unique id each call', () => {
    const a = createDefaultConfig();
    const b = createDefaultConfig();
    expect(a.id).not.toBe(b.id);
  });

  it('sets sensible defaults', () => {
    const cfg = createDefaultConfig();
    expect(cfg.chartType).toBe('bar');
    expect(cfg.dimensions).toEqual([]);
    expect(cfg.measures).toEqual([]);
    expect(cfg.metrics).toEqual([]);
    expect(cfg.filters).toEqual([]);
    expect(cfg.sorts).toEqual([]);
    expect(cfg.limit).toBe(1000);
    expect(cfg.datasetId).toBe('');
  });

  it('merges overrides', () => {
    const cfg = createDefaultConfig({ datasetId: 'ds-1', limit: 500, chartType: 'line' });
    expect(cfg.datasetId).toBe('ds-1');
    expect(cfg.limit).toBe(500);
    expect(cfg.chartType).toBe('line');
    // Defaults still present where not overridden
    expect(cfg.dimensions).toEqual([]);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// isConfigValid
// ─────────────────────────────────────────────────────────────────────────────

function makeValid(overrides?: Partial<ChartConfig>): ChartConfig {
  return createDefaultConfig({
    datasetId: 'ds-1',
    chartType: 'bar',
    dimensions: ['category'],
    measures: ['revenue'],
    ...overrides,
  });
}

describe('isConfigValid', () => {
  it('returns false when datasetId is empty', () => {
    expect(isConfigValid(makeValid({ datasetId: '' }))).toBe(false);
  });

  it('returns false when no fields selected', () => {
    expect(
      isConfigValid(makeValid({ dimensions: [], measures: [], metrics: [] }))
    ).toBe(false);
  });

  it('returns false when bar chart has no dimensions (minDimensions = 1)', () => {
    expect(
      isConfigValid(makeValid({ chartType: 'bar', dimensions: [], measures: ['revenue'] }))
    ).toBe(false);
  });

  it('returns false when bar chart has no measures (minMeasures = 1)', () => {
    expect(
      isConfigValid(makeValid({ chartType: 'bar', dimensions: ['category'], measures: [] }))
    ).toBe(false);
  });

  it('returns true for a complete bar config', () => {
    expect(isConfigValid(makeValid())).toBe(true);
  });

  it('returns true for scatter with 2 measures and 0 dimensions (minDimensions = 0)', () => {
    expect(
      isConfigValid(
        makeValid({ chartType: 'scatter', dimensions: [], measures: ['x', 'y'] })
      )
    ).toBe(true);
  });

  it('returns true for table with only dimensions (minMeasures = 0)', () => {
    expect(
      isConfigValid(
        makeValid({ chartType: 'table', dimensions: ['name'], measures: [] })
      )
    ).toBe(true);
  });

  it('returns true when metrics compensate for missing measures in kpi', () => {
    expect(
      isConfigValid(
        makeValid({ chartType: 'kpi', dimensions: [], measures: [], metrics: ['conversion_rate'] })
      )
    ).toBe(true);
  });

  it('returns false for unknown chartType', () => {
    // @ts-expect-error testing runtime behaviour
    expect(isConfigValid(makeValid({ chartType: 'unknown_type' }))).toBe(false);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// CHART_TYPE_META — sanity checks
// ─────────────────────────────────────────────────────────────────────────────

describe('CHART_TYPE_META', () => {
  it('contains all expected chart types', () => {
    const types = CHART_TYPE_META.map((m) => m.type);
    expect(types).toContain('bar');
    expect(types).toContain('line');
    expect(types).toContain('pie');
    expect(types).toContain('donut');
    expect(types).toContain('scatter');
    expect(types).toContain('table');
    expect(types).toContain('kpi');
  });

  it('every meta entry has required fields', () => {
    for (const m of CHART_TYPE_META) {
      expect(typeof m.label).toBe('string');
      expect(typeof m.icon).toBe('string');
      expect(typeof m.minDimensions).toBe('number');
      expect(typeof m.minMeasures).toBe('number');
      expect(typeof m.supportsTimeDimension).toBe('boolean');
    }
  });

  it('bar and line support time dimension', () => {
    const bar = CHART_TYPE_META.find((m) => m.type === 'bar');
    const line = CHART_TYPE_META.find((m) => m.type === 'line');
    expect(bar?.supportsTimeDimension).toBe(true);
    expect(line?.supportsTimeDimension).toBe(true);
  });

  it('pie and scatter do not support time dimension', () => {
    const pie = CHART_TYPE_META.find((m) => m.type === 'pie');
    const scatter = CHART_TYPE_META.find((m) => m.type === 'scatter');
    expect(pie?.supportsTimeDimension).toBe(false);
    expect(scatter?.supportsTimeDimension).toBe(false);
  });
});
