import React from 'react';
import { render, screen } from '@testing-library/react';
import { KPIRenderer } from '../../../../src/frontend/src/shared/components/charts/KPIRenderer';
import { createDefaultConfig } from '../../../../src/frontend/src/features/chart-builder/types';
import type { QueryResultParsed } from '../../../../src/frontend/src/graphql/types';

// ─────────────────────────────────────────────────────────────────────────────
// KPIRenderer — single metric card
// ─────────────────────────────────────────────────────────────────────────────

function makeData(rows: Record<string, unknown>[], colName = 'revenue'): QueryResultParsed {
  return {
    columns: [
      {
        name: colName,
        displayName: 'Revenue',
        dataType: 'numeric',
        fieldType: 'measure',
      },
    ],
    rows: rows as QueryResultParsed['rows'],
    metadata: {
      generatedSql: '',
      executionTimeMs: 5,
      totalRows: rows.length,
      fromCache: false,
      executedAt: new Date().toISOString(),
    },
  };
}

const config = createDefaultConfig({
  chartType: 'kpi',
  datasetId: 'ds-1',
  measures: ['revenue'],
});

describe('KPIRenderer', () => {
  it('renders the column display name as label', () => {
    render(<KPIRenderer data={makeData([{ revenue: 1000 }])} config={config} />);
    expect(screen.getByText('Revenue')).toBeInTheDocument();
  });

  it('renders the summed value', () => {
    render(
      <KPIRenderer
        data={makeData([{ revenue: 500 }, { revenue: 500 }])}
        config={config}
      />
    );
    expect(screen.getByText('1K')).toBeInTheDocument();
  });

  it('renders — for null value', () => {
    render(<KPIRenderer data={makeData([{ revenue: null }])} config={config} />);
    // null rows contribute 0
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('renders prefix and suffix from visualOptions', () => {
    const configWithOptions = createDefaultConfig({
      chartType: 'kpi',
      datasetId: 'ds-1',
      measures: ['revenue'],
      visualOptions: { prefix: '$', suffix: ' USD' },
    });
    render(<KPIRenderer data={makeData([{ revenue: 1000 }])} config={configWithOptions} />);
    expect(screen.getByText(/\$.*1K.* USD/)).toBeInTheDocument();
  });

  it('renders multi-record indicator when more than 1 row', () => {
    render(
      <KPIRenderer
        data={makeData([{ revenue: 100 }, { revenue: 200 }])}
        config={config}
      />
    );
    expect(screen.getByText(/2 records/)).toBeInTheDocument();
  });

  it('formats millions correctly', () => {
    render(<KPIRenderer data={makeData([{ revenue: 5_200_000 }])} config={config} />);
    expect(screen.getByText('5.2M')).toBeInTheDocument();
  });

  it('formats billions correctly', () => {
    render(<KPIRenderer data={makeData([{ revenue: 2_100_000_000 }])} config={config} />);
    expect(screen.getByText('2.1B')).toBeInTheDocument();
  });
});
