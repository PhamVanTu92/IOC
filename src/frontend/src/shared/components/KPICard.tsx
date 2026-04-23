import React from 'react';

// ─────────────────────────────────────────────────────────────────────────────
// KPICard — Widget hiển thị chỉ số KPI đơn
// ─────────────────────────────────────────────────────────────────────────────

export type TrendDirection = 'up' | 'down' | 'neutral';

export interface KPICardProps {
  title: string;
  value: number | string;
  unit?: string;
  trend?: {
    direction: TrendDirection;
    percent: number;
    label?: string;
  };
  icon?: string;
  color?: string;
  loading?: boolean;
  className?: string;
}

function formatNumber(value: number | string): string {
  if (typeof value === 'string') return value;
  if (value >= 1_000_000_000) return `${(value / 1_000_000_000).toFixed(1)}B`;
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(1)}K`;
  return value.toLocaleString('vi-VN');
}

const TREND_ICONS: Record<TrendDirection, string> = {
  up: '↑',
  down: '↓',
  neutral: '→',
};

const TREND_COLORS: Record<TrendDirection, string> = {
  up: '#10B981',
  down: '#EF4444',
  neutral: '#6B7280',
};

export function KPICard({
  title,
  value,
  unit,
  trend,
  icon,
  color = '#3B82F6',
  loading = false,
  className = '',
}: KPICardProps) {
  if (loading) {
    return (
      <div className={`ioc-kpi-card ioc-kpi-card--loading ${className}`}>
        <div className="ioc-skeleton ioc-skeleton--title" />
        <div className="ioc-skeleton ioc-skeleton--value" />
      </div>
    );
  }

  return (
    <div
      className={`ioc-kpi-card ${className}`}
      style={{ '--kpi-color': color } as React.CSSProperties}
    >
      <div className="ioc-kpi-card__header">
        {icon && (
          <div className="ioc-kpi-card__icon" style={{ backgroundColor: `${color}20` }}>
            <span style={{ color }}>{icon}</span>
          </div>
        )}
        <p className="ioc-kpi-card__title">{title}</p>
      </div>

      <div className="ioc-kpi-card__value">
        <span className="ioc-kpi-card__number">{formatNumber(value)}</span>
        {unit && <span className="ioc-kpi-card__unit">{unit}</span>}
      </div>

      {trend && (
        <div
          className="ioc-kpi-card__trend"
          style={{ color: TREND_COLORS[trend.direction] }}
        >
          <span>{TREND_ICONS[trend.direction]}</span>
          <span>{trend.percent.toFixed(1)}%</span>
          {trend.label && <span className="ioc-kpi-card__trend-label">{trend.label}</span>}
        </div>
      )}
    </div>
  );
}
