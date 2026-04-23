// ─────────────────────────────────────────────────────────────────────────────
// IOC Chart Theme — centralized color palette and ECharts defaults
// ─────────────────────────────────────────────────────────────────────────────

export const IOC_COLORS = [
  '#3b82f6', // blue-500
  '#10b981', // emerald-500
  '#f59e0b', // amber-500
  '#ef4444', // red-500
  '#8b5cf6', // violet-500
  '#06b6d4', // cyan-500
  '#f97316', // orange-500
  '#84cc16', // lime-500
  '#ec4899', // pink-500
  '#6366f1', // indigo-500
];

export const IOC_GRID = {
  top: 40,
  right: 20,
  bottom: 60,
  left: 60,
  containLabel: true,
};

export const IOC_TOOLTIP = {
  trigger: 'axis' as const,
  backgroundColor: '#1f2937',
  borderColor: '#374151',
  textStyle: { color: '#f9fafb', fontSize: 12 },
  borderWidth: 1,
};

export const IOC_LEGEND = {
  type: 'scroll' as const,
  bottom: 0,
  textStyle: { color: '#6b7280', fontSize: 12 },
};

export const IOC_AXIS_LABEL = {
  color: '#6b7280',
  fontSize: 12,
};

export const IOC_AXIS_LINE = {
  lineStyle: { color: '#374151' },
};

export const IOC_SPLIT_LINE = {
  lineStyle: { color: '#1f2937', type: 'dashed' as const },
};

export function resolveColors(palette?: string[]): string[] {
  return palette && palette.length > 0 ? palette : IOC_COLORS;
}
