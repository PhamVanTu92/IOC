import React from 'react';
import type { IOCPlugin } from '@core/PluginRegistry';

// ─────────────────────────────────────────────────────────────────────────────
// Finance Plugin — Tài chính
// ─────────────────────────────────────────────────────────────────────────────

export const FinancePlugin: IOCPlugin = {
  id: 'finance',
  name: 'Tài Chính',
  version: '1.0.0',
  description: 'Quản lý ngân sách, thu chi, báo cáo tài chính',
  icon: '💰',
  color: '#10B981',

  routes: [
    {
      path: '/finance',
      label: 'Tổng quan Tài chính',
      component: React.lazy(() => import('./pages/FinanceOverview')),
    },
    {
      path: '/finance/budget',
      label: 'Ngân sách',
      component: React.lazy(() => import('./pages/BudgetPage')),
    },
    {
      path: '/finance/reports',
      label: 'Báo cáo',
      component: React.lazy(() => import('./pages/FinanceReports')),
    },
  ],

  widgets: [
    {
      id: 'finance.revenue-chart',
      name: 'Biểu đồ Doanh thu',
      description: 'Line chart doanh thu theo tháng',
      defaultSize: { w: 6, h: 3 },
      category: 'chart',
      component: React.lazy(() => import('./widgets/RevenueChart')),
    },
    {
      id: 'finance.budget-kpi',
      name: 'KPI Ngân sách',
      description: 'Tổng ngân sách đã dùng / còn lại',
      defaultSize: { w: 3, h: 2 },
      category: 'kpi',
      component: React.lazy(() => import('./widgets/BudgetKPI')),
    },
    {
      id: 'finance.expense-pie',
      name: 'Phân bổ Chi phí',
      description: 'Pie chart phân bổ theo danh mục',
      defaultSize: { w: 4, h: 3 },
      category: 'chart',
      component: React.lazy(() => import('./widgets/ExpensePie')),
    },
  ],

  menuItems: [
    {
      id: 'finance-menu',
      label: 'Tài Chính',
      icon: '💰',
      path: '/finance',
      order: 10,
      children: [
        { id: 'finance-budget', label: 'Ngân sách', icon: '📊', path: '/finance/budget', order: 11 },
        { id: 'finance-reports', label: 'Báo cáo', icon: '📄', path: '/finance/reports', order: 12 },
      ],
    },
  ],

  onInit: async () => {
    console.info('[Finance Plugin] Initialized');
  },
};
