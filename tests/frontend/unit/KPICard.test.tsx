import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { KPICard } from '../../../src/frontend/src/shared/components/KPICard';

describe('KPICard', () => {
  it('hiển thị title và value', () => {
    render(<KPICard title="Doanh thu" value={1500000} />);
    expect(screen.getByText('Doanh thu')).toBeInTheDocument();
    expect(screen.getByText('1.5M')).toBeInTheDocument();
  });

  it('hiển thị unit nếu có', () => {
    render(<KPICard title="Tỷ lệ" value={85} unit="%" />);
    expect(screen.getByText('%')).toBeInTheDocument();
  });

  it('hiển thị trend up với màu xanh', () => {
    render(
      <KPICard
        title="Revenue"
        value={1000000}
        trend={{ direction: 'up', percent: 12.5, label: 'so tháng trước' }}
      />
    );
    expect(screen.getByText('↑')).toBeInTheDocument();
    expect(screen.getByText('12.5%')).toBeInTheDocument();
  });

  it('hiển thị trend down với màu đỏ', () => {
    render(
      <KPICard
        title="Chi phí"
        value={500000}
        trend={{ direction: 'down', percent: 5.2 }}
      />
    );
    expect(screen.getByText('↓')).toBeInTheDocument();
  });

  it('hiển thị loading skeleton khi loading=true', () => {
    const { container } = render(<KPICard title="Test" value={0} loading />);
    expect(container.querySelector('.ioc-kpi-card--loading')).toBeInTheDocument();
    expect(screen.queryByText('Test')).not.toBeInTheDocument();
  });

  it('format số lớn thành K/M/B', () => {
    const { rerender } = render(<KPICard title="T" value={1000} />);
    expect(screen.getByText('1.0K')).toBeInTheDocument();

    rerender(<KPICard title="T" value={2500000} />);
    expect(screen.getByText('2.5M')).toBeInTheDocument();

    rerender(<KPICard title="T" value={1200000000} />);
    expect(screen.getByText('1.2B')).toBeInTheDocument();
  });
});
