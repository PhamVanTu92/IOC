import React from 'react';
import type { IOCPlugin } from '@core/PluginRegistry';

// ─────────────────────────────────────────────────────────────────────────────
// Marketing Plugin
// ─────────────────────────────────────────────────────────────────────────────

export const MarketingPlugin: IOCPlugin = {
  id: 'marketing',
  name: 'Marketing',
  version: '1.0.0',
  description: 'Quản lý campaign, leads, conversion, ROI marketing',
  icon: '📣',
  color: '#F59E0B',

  routes: [
    {
      path: '/marketing',
      label: 'Tổng quan Marketing',
      component: React.lazy(() => import('./pages/MarketingOverview')),
    },
    {
      path: '/marketing/campaigns',
      label: 'Campaigns',
      component: React.lazy(() => import('./pages/CampaignsPage')),
    },
    {
      path: '/marketing/leads',
      label: 'Leads',
      component: React.lazy(() => import('./pages/LeadsPage')),
    },
  ],

  widgets: [
    {
      id: 'marketing.campaign-kpi',
      name: 'KPI Campaign',
      description: 'Số campaign đang chạy, leads, conversion rate',
      defaultSize: { w: 3, h: 2 },
      category: 'kpi',
      component: React.lazy(() => import('./widgets/CampaignKPI')),
    },
    {
      id: 'marketing.funnel-chart',
      name: 'Funnel Marketing',
      description: 'Phễu chuyển đổi từ lead → customer',
      defaultSize: { w: 5, h: 4 },
      category: 'chart',
      component: React.lazy(() => import('./widgets/FunnelChart')),
    },
    {
      id: 'marketing.channel-bar',
      name: 'Kênh Marketing',
      description: 'Hiệu quả theo kênh (Social, Email, SEO, Ads)',
      defaultSize: { w: 6, h: 3 },
      category: 'chart',
      component: React.lazy(() => import('./widgets/ChannelBar')),
    },
  ],

  menuItems: [
    {
      id: 'marketing-menu',
      label: 'Marketing',
      icon: '📣',
      path: '/marketing',
      order: 30,
      children: [
        { id: 'marketing-campaigns', label: 'Campaigns', icon: '🎯', path: '/marketing/campaigns', order: 31 },
        { id: 'marketing-leads', label: 'Leads', icon: '🧲', path: '/marketing/leads', order: 32 },
      ],
    },
  ],

  onInit: async () => {
    console.info('[Marketing Plugin] Initialized');
  },
};
