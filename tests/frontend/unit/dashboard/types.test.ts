import {
  createDefaultDashboard,
  createWidget,
  widgetHeightPx,
  gridColumnSpan,
  ROW_HEIGHT_PX,
  WIDGET_SIZE_PRESETS,
} from '../../../../src/frontend/src/features/dashboard/types';
import { createDefaultConfig } from '../../../../src/frontend/src/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// dashboard/types.ts
// ─────────────────────────────────────────────────────────────────────────────

describe('createDefaultDashboard', () => {
  it('generates unique ids', () => {
    const a = createDefaultDashboard();
    const b = createDefaultDashboard();
    expect(a.id).not.toBe(b.id);
  });

  it('starts with empty widgets array', () => {
    expect(createDefaultDashboard().widgets).toEqual([]);
  });

  it('applies overrides', () => {
    const d = createDefaultDashboard({ title: 'Finance', description: 'desc' });
    expect(d.title).toBe('Finance');
    expect(d.description).toBe('desc');
  });

  it('sets createdAt and updatedAt as ISO strings', () => {
    const d = createDefaultDashboard();
    expect(() => new Date(d.createdAt)).not.toThrow();
    expect(() => new Date(d.updatedAt)).not.toThrow();
  });
});

describe('createWidget', () => {
  const chartConfig = createDefaultConfig({ datasetId: 'ds-1', title: 'Revenue' });

  it('generates a unique id', () => {
    const a = createWidget(chartConfig);
    const b = createWidget(chartConfig);
    expect(a.id).not.toBe(b.id);
  });

  it('assigns chartConfig correctly', () => {
    const w = createWidget(chartConfig);
    expect(w.chartConfig).toBe(chartConfig);
  });

  it('uses medium preset as default layout', () => {
    const w = createWidget(chartConfig);
    expect(w.layout.w).toBe(WIDGET_SIZE_PRESETS.medium.w);
    expect(w.layout.h).toBe(WIDGET_SIZE_PRESETS.medium.h);
  });

  it('merges layout overrides', () => {
    const w = createWidget(chartConfig, { w: 12, h: 4 });
    expect(w.layout.w).toBe(12);
    expect(w.layout.h).toBe(4);
  });

  it('defaults x and y to 0', () => {
    const w = createWidget(chartConfig);
    expect(w.layout.x).toBe(0);
    expect(w.layout.y).toBe(0);
  });
});

describe('widgetHeightPx', () => {
  it('multiplies h by ROW_HEIGHT_PX', () => {
    expect(widgetHeightPx(1)).toBe(ROW_HEIGHT_PX);
    expect(widgetHeightPx(3)).toBe(3 * ROW_HEIGHT_PX);
    expect(widgetHeightPx(0)).toBe(0);
  });
});

describe('gridColumnSpan', () => {
  it('returns a CSS span string', () => {
    expect(gridColumnSpan(6)).toBe('span 6');
    expect(gridColumnSpan(12)).toBe('span 12');
    expect(gridColumnSpan(1)).toBe('span 1');
  });
});

describe('WIDGET_SIZE_PRESETS', () => {
  it('all presets have valid w (1-12) and h (>=1)', () => {
    for (const [, size] of Object.entries(WIDGET_SIZE_PRESETS)) {
      expect(size.w).toBeGreaterThanOrEqual(1);
      expect(size.w).toBeLessThanOrEqual(12);
      expect(size.h).toBeGreaterThanOrEqual(1);
    }
  });

  it('full preset spans all 12 columns', () => {
    expect(WIDGET_SIZE_PRESETS.full.w).toBe(12);
  });

  it('small preset is smaller than medium (w)', () => {
    expect(WIDGET_SIZE_PRESETS.small.w).toBeLessThan(WIDGET_SIZE_PRESETS.medium.w);
  });
});
