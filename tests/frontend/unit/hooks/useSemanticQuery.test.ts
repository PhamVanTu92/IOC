import {
  getColumnValues,
  getNumericValues,
  getStringValues,
} from '../../../../src/frontend/src/shared/hooks/useSemanticQuery';
import type { ParsedRow } from '../../../../src/frontend/src/graphql/types';

// ─────────────────────────────────────────────────────────────────────────────
// useSemanticQuery utility functions
// ─────────────────────────────────────────────────────────────────────────────

const rows: ParsedRow[] = [
  { city: 'Hà Nội', revenue: 1000, active: true, missing: null },
  { city: 'TP HCM', revenue: 2500.5, active: false, missing: null },
  { city: 'Đà Nẵng', revenue: 0, active: true, missing: null },
  { city: null, revenue: null, active: null, missing: null },
];

describe('getColumnValues', () => {
  it('extracts all values including nulls', () => {
    expect(getColumnValues(rows, 'city')).toEqual(['Hà Nội', 'TP HCM', 'Đà Nẵng', null]);
  });

  it('returns null for missing column key', () => {
    expect(getColumnValues(rows, 'nonexistent')).toEqual([null, null, null, null]);
  });

  it('handles null values in column', () => {
    expect(getColumnValues(rows, 'revenue')).toEqual([1000, 2500.5, 0, null]);
  });
});

describe('getNumericValues', () => {
  it('returns only numeric values, excluding nulls', () => {
    expect(getNumericValues(rows, 'revenue')).toEqual([1000, 2500.5, 0]);
  });

  it('coerces numeric strings', () => {
    const r: ParsedRow[] = [{ v: '42' }, { v: '3.14' }, { v: 'abc' }, { v: null }];
    const result = getNumericValues(r, 'v');
    expect(result).toContain(42);
    expect(result).toContain(3.14);
    expect(result).toHaveLength(2); // 'abc' is NaN, null filtered
  });

  it('returns empty array when no numeric values', () => {
    const r: ParsedRow[] = [{ v: null }, { v: null }];
    expect(getNumericValues(r, 'v')).toEqual([]);
  });

  it('returns empty array for empty rows', () => {
    expect(getNumericValues([], 'revenue')).toEqual([]);
  });
});

describe('getStringValues', () => {
  it('coerces all non-null values to strings', () => {
    expect(getStringValues(rows, 'revenue')).toEqual(['1000', '2500.5', '0']);
  });

  it('excludes null and undefined', () => {
    expect(getStringValues(rows, 'city')).toEqual(['Hà Nội', 'TP HCM', 'Đà Nẵng']);
  });

  it('coerces boolean values to strings', () => {
    const nonNull = getStringValues(rows, 'active');
    expect(nonNull).toEqual(['true', 'false', 'true']);
  });

  it('returns empty array for all-null column', () => {
    expect(getStringValues(rows, 'missing')).toEqual([]);
  });

  it('returns empty array for empty rows', () => {
    expect(getStringValues([], 'city')).toEqual([]);
  });
});
