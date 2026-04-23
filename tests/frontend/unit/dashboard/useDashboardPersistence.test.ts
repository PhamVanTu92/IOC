import { listLocalDashboards } from '../../../../src/frontend/src/features/dashboard/useDashboardPersistence';
import { createDefaultDashboard, createWidget } from '../../../../src/frontend/src/features/dashboard/types';
import { createDefaultConfig } from '../../../../src/frontend/src/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// useDashboardPersistence — LocalStorage utilities
// Apollo mutation hooks require MockedProvider; tested via integration tests.
// Here we test the LS utility functions exported for offline access.
// ─────────────────────────────────────────────────────────────────────────────

// Mock localStorage
const mockStorage: Record<string, string> = {};

beforeEach(() => {
  // Clear mock storage before each test
  Object.keys(mockStorage).forEach((k) => delete mockStorage[k]);
  Object.defineProperty(window, 'localStorage', {
    value: {
      getItem: (k: string) => mockStorage[k] ?? null,
      setItem: (k: string, v: string) => { mockStorage[k] = v; },
      removeItem: (k: string) => { delete mockStorage[k]; },
      clear: () => { Object.keys(mockStorage).forEach((k) => delete mockStorage[k]); },
      length: 0,
      key: () => null,
    },
    writable: true,
  });
});

function saveDashboardToLS(config: ReturnType<typeof createDefaultDashboard>) {
  const key = `ioc:dashboard:${config.id}`;
  mockStorage[key] = JSON.stringify(config);
  const indexRaw = mockStorage['ioc:dashboard:_index'];
  const index = indexRaw ? (JSON.parse(indexRaw) as string[]) : [];
  if (!index.includes(config.id)) {
    mockStorage['ioc:dashboard:_index'] = JSON.stringify([...index, config.id]);
  }
}

describe('listLocalDashboards', () => {
  it('returns empty array when no dashboards stored', () => {
    expect(listLocalDashboards()).toEqual([]);
  });

  it('returns stored dashboards', () => {
    const d1 = createDefaultDashboard({ title: 'Finance' });
    const d2 = createDefaultDashboard({ title: 'HR' });
    saveDashboardToLS(d1);
    saveDashboardToLS(d2);

    const result = listLocalDashboards();
    expect(result).toHaveLength(2);
    const titles = result.map((d) => d.title);
    expect(titles).toContain('Finance');
    expect(titles).toContain('HR');
  });

  it('ignores malformed JSON entries', () => {
    const validDash = createDefaultDashboard({ title: 'Valid' });
    saveDashboardToLS(validDash);
    // Inject malformed JSON
    mockStorage[`ioc:dashboard:bad-id`] = '{not-valid-json}';
    mockStorage['ioc:dashboard:_index'] = JSON.stringify([validDash.id, 'bad-id']);

    const result = listLocalDashboards();
    expect(result).toHaveLength(1);
    expect(result[0].title).toBe('Valid');
  });

  it('preserves widgets in stored config', () => {
    const d = createDefaultDashboard({ title: 'Board' });
    const widget = createWidget(createDefaultConfig({ title: 'Revenue' }));
    d.widgets.push(widget);
    saveDashboardToLS(d);

    const result = listLocalDashboards();
    expect(result[0].widgets).toHaveLength(1);
    expect(result[0].widgets[0].chartConfig.title).toBe('Revenue');
  });
});
