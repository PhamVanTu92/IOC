import { act } from '@testing-library/react';
import { useDashboardStore } from '../../../../src/frontend/src/features/dashboard/useDashboardStore';
import { createDefaultConfig } from '../../../../src/frontend/src/features/chart-builder/types';
import { createDefaultDashboard, createWidget } from '../../../../src/frontend/src/features/dashboard/types';

// ─────────────────────────────────────────────────────────────────────────────
// useDashboardStore — Zustand store
// We interact with the store directly (getState / setState) — no React needed
// ─────────────────────────────────────────────────────────────────────────────

function getStore() {
  return useDashboardStore.getState();
}

function resetStore() {
  act(() => {
    useDashboardStore.getState().resetDashboard();
  });
}

const makeChart = (title = 'Chart') =>
  createDefaultConfig({ datasetId: 'ds-1', title });

beforeEach(resetStore);

describe('initial state', () => {
  it('starts with empty widgets', () => {
    expect(getStore().dashboard.widgets).toHaveLength(0);
  });

  it('starts with isDirty = false', () => {
    expect(getStore().isDirty).toBe(false);
  });

  it('starts in edit mode (new dashboard)', () => {
    expect(getStore().editMode).toBe(true);
  });

  it('starts with no editingWidgetId', () => {
    expect(getStore().editingWidgetId).toBeNull();
  });
});

describe('loadDashboard', () => {
  it('replaces current dashboard and clears dirty/editMode', () => {
    const cfg = createDefaultDashboard({ title: 'Loaded' });
    act(() => getStore().loadDashboard(cfg));
    expect(getStore().dashboard.title).toBe('Loaded');
    expect(getStore().isDirty).toBe(false);
    expect(getStore().editMode).toBe(false);
  });
});

describe('setTitle / setDescription', () => {
  it('setTitle updates title and marks dirty', () => {
    act(() => getStore().setTitle('Finance Dashboard'));
    expect(getStore().dashboard.title).toBe('Finance Dashboard');
    expect(getStore().isDirty).toBe(true);
  });

  it('setDescription updates description', () => {
    act(() => getStore().setDescription('Monthly KPIs'));
    expect(getStore().dashboard.description).toBe('Monthly KPIs');
  });
});

describe('editMode', () => {
  it('toggleEditMode flips state', () => {
    act(() => getStore().setEditMode(false));
    act(() => getStore().toggleEditMode());
    expect(getStore().editMode).toBe(true);
    act(() => getStore().toggleEditMode());
    expect(getStore().editMode).toBe(false);
  });

  it('setEditMode sets explicit value', () => {
    act(() => getStore().setEditMode(false));
    expect(getStore().editMode).toBe(false);
    act(() => getStore().setEditMode(true));
    expect(getStore().editMode).toBe(true);
  });
});

describe('addWidget', () => {
  it('appends widget to dashboard', () => {
    act(() => getStore().addWidget(makeChart('Revenue')));
    expect(getStore().dashboard.widgets).toHaveLength(1);
    expect(getStore().dashboard.widgets[0].chartConfig.title).toBe('Revenue');
  });

  it('marks isDirty', () => {
    act(() => getStore().addWidget(makeChart()));
    expect(getStore().isDirty).toBe(true);
  });

  it('stacks widgets vertically (y = previous maxY)', () => {
    act(() => getStore().addWidget(makeChart('A'), { w: 12, h: 3 }));
    act(() => getStore().addWidget(makeChart('B'), { w: 12, h: 2 }));
    const widgets = getStore().dashboard.widgets;
    expect(widgets[0].layout.y).toBe(0);
    expect(widgets[1].layout.y).toBe(3); // after first widget's h
  });

  it('applies custom layout', () => {
    act(() => getStore().addWidget(makeChart(), { w: 4, h: 2 }));
    const w = getStore().dashboard.widgets[0];
    expect(w.layout.w).toBe(4);
    expect(w.layout.h).toBe(2);
  });
});

describe('removeWidget', () => {
  it('removes the widget by id', () => {
    act(() => getStore().addWidget(makeChart('A')));
    act(() => getStore().addWidget(makeChart('B')));
    const id = getStore().dashboard.widgets[0].id;
    act(() => getStore().removeWidget(id));
    expect(getStore().dashboard.widgets).toHaveLength(1);
    expect(getStore().dashboard.widgets[0].chartConfig.title).toBe('B');
  });

  it('does nothing for unknown id', () => {
    act(() => getStore().addWidget(makeChart()));
    act(() => getStore().removeWidget('nonexistent-id'));
    expect(getStore().dashboard.widgets).toHaveLength(1);
  });
});

describe('updateWidgetConfig', () => {
  it('updates chartConfig of the specified widget', () => {
    act(() => getStore().addWidget(makeChart('Old')));
    const id = getStore().dashboard.widgets[0].id;
    const newConfig = makeChart('New');
    act(() => getStore().updateWidgetConfig(id, newConfig));
    expect(getStore().dashboard.widgets[0].chartConfig.title).toBe('New');
  });
});

describe('resizeWidget', () => {
  it('applies preset dimensions', () => {
    act(() => getStore().addWidget(makeChart()));
    const id = getStore().dashboard.widgets[0].id;
    act(() => getStore().resizeWidget(id, 'full'));
    const layout = getStore().dashboard.widgets[0].layout;
    expect(layout.w).toBe(12);
    expect(layout.h).toBeGreaterThanOrEqual(2);
  });

  it('marks dirty', () => {
    act(() => getStore().addWidget(makeChart()));
    const id = getStore().dashboard.widgets[0].id;
    // reset dirty
    act(() => getStore().markSaved());
    act(() => getStore().resizeWidget(id, 'small'));
    expect(getStore().isDirty).toBe(true);
  });
});

describe('reorderWidgets', () => {
  it('reorders widgets by id list', () => {
    act(() => getStore().addWidget(makeChart('A')));
    act(() => getStore().addWidget(makeChart('B')));
    act(() => getStore().addWidget(makeChart('C')));
    const ids = getStore().dashboard.widgets.map((w) => w.id);
    // Reverse order
    act(() => getStore().reorderWidgets([ids[2], ids[1], ids[0]]));
    const titles = getStore().dashboard.widgets.map((w) => w.chartConfig.title);
    expect(titles).toEqual(['C', 'B', 'A']);
  });

  it('handles unknown ids gracefully (filters them out)', () => {
    act(() => getStore().addWidget(makeChart('A')));
    const id = getStore().dashboard.widgets[0].id;
    act(() => getStore().reorderWidgets([id, 'ghost-id']));
    expect(getStore().dashboard.widgets).toHaveLength(1);
  });
});

describe('modal (openEditor / closeEditor)', () => {
  it('openEditor sets editingWidgetId', () => {
    act(() => getStore().openEditor('widget-abc'));
    expect(getStore().editingWidgetId).toBe('widget-abc');
  });

  it('closeEditor clears editingWidgetId', () => {
    act(() => getStore().openEditor('widget-abc'));
    act(() => getStore().closeEditor());
    expect(getStore().editingWidgetId).toBeNull();
  });
});

describe('markSaved', () => {
  it('clears isDirty', () => {
    act(() => getStore().setTitle('Changed'));
    expect(getStore().isDirty).toBe(true);
    act(() => getStore().markSaved());
    expect(getStore().isDirty).toBe(false);
  });
});
