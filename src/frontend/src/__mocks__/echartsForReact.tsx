import React from 'react';

// Lightweight stub for echarts-for-react in Jest
// Renders a <div data-testid="echart"> so tests can assert presence
interface ReactEChartsProps {
  option?: unknown;
  style?: React.CSSProperties;
  className?: string;
  notMerge?: boolean;
  lazyUpdate?: boolean;
}

function ReactECharts({ style, className }: ReactEChartsProps) {
  return <div data-testid="echart" style={style} className={className} />;
}

export default ReactECharts;
