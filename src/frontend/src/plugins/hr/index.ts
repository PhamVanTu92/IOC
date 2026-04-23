import React from 'react';
import type { IOCPlugin } from '@core/PluginRegistry';

// ─────────────────────────────────────────────────────────────────────────────
// HR Plugin — Nhân sự
// ─────────────────────────────────────────────────────────────────────────────

export const HRPlugin: IOCPlugin = {
  id: 'hr',
  name: 'Nhân Sự',
  version: '1.0.0',
  description: 'Quản lý nhân viên, chấm công, nghỉ phép, lương thưởng',
  icon: '👥',
  color: '#3B82F6',

  routes: [
    {
      path: '/hr',
      label: 'Tổng quan Nhân sự',
      component: React.lazy(() => import('./pages/HROverview')),
    },
    {
      path: '/hr/employees',
      label: 'Nhân viên',
      component: React.lazy(() => import('./pages/EmployeesPage')),
    },
    {
      path: '/hr/attendance',
      label: 'Chấm công',
      component: React.lazy(() => import('./pages/AttendancePage')),
    },
    {
      path: '/hr/payroll',
      label: 'Lương',
      component: React.lazy(() => import('./pages/PayrollPage')),
    },
  ],

  widgets: [
    {
      id: 'hr.headcount-kpi',
      name: 'KPI Nhân sự',
      description: 'Tổng nhân viên, tuyển mới, nghỉ việc',
      defaultSize: { w: 3, h: 2 },
      category: 'kpi',
      component: React.lazy(() => import('./widgets/HeadcountKPI')),
    },
    {
      id: 'hr.attendance-chart',
      name: 'Chấm công',
      description: 'Bar chart tỷ lệ đi làm / vắng mặt theo tuần',
      defaultSize: { w: 6, h: 3 },
      category: 'chart',
      component: React.lazy(() => import('./widgets/AttendanceChart')),
    },
    {
      id: 'hr.department-pie',
      name: 'Cơ cấu Phòng ban',
      description: 'Phân bổ nhân sự theo phòng ban',
      defaultSize: { w: 4, h: 3 },
      category: 'chart',
      component: React.lazy(() => import('./widgets/DepartmentPie')),
    },
  ],

  menuItems: [
    {
      id: 'hr-menu',
      label: 'Nhân Sự',
      icon: '👥',
      path: '/hr',
      order: 20,
      children: [
        { id: 'hr-employees', label: 'Nhân viên', icon: '👤', path: '/hr/employees', order: 21 },
        { id: 'hr-attendance', label: 'Chấm công', icon: '📅', path: '/hr/attendance', order: 22 },
        { id: 'hr-payroll', label: 'Lương', icon: '💳', path: '/hr/payroll', order: 23 },
      ],
    },
  ],

  onInit: async () => {
    console.info('[HR Plugin] Initialized');
  },
};
