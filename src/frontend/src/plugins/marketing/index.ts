import type { IOCPlugin } from '@core/PluginRegistry';
import { ComingSoon } from '@shared/components/ComingSoon';

const placeholder = (name: string) => () => ComingSoon({ name });

export const MarketingPlugin: IOCPlugin = {
  id: 'marketing',
  name: 'Marketing',
  version: '1.0.0',
  description: 'Quản lý campaign, leads, conversion, ROI marketing',
  icon: '📣',
  color: '#F59E0B',

  routes: [
    { path: '/marketing',           label: 'Tổng quan Marketing', component: placeholder('Marketing Overview') },
    { path: '/marketing/campaigns', label: 'Campaigns',            component: placeholder('Campaigns') },
    { path: '/marketing/leads',     label: 'Leads',                component: placeholder('Leads') },
  ],

  widgets: [
    {
      id: 'marketing.campaign-kpi',
      name: 'KPI Campaign',
      description: 'Số campaign đang chạy, leads, conversion rate',
      defaultSize: { w: 3, h: 2 },
      category: 'kpi',
      component: placeholder('Campaign KPI'),
    },
    {
      id: 'marketing.funnel-chart',
      name: 'Funnel Marketing',
      description: 'Phễu chuyển đổi từ lead → customer',
      defaultSize: { w: 5, h: 4 },
      category: 'chart',
      component: placeholder('Funnel Chart'),
    },
    {
      id: 'marketing.channel-bar',
      name: 'Kênh Marketing',
      description: 'Hiệu quả theo kênh (Social, Email, SEO, Ads)',
      defaultSize: { w: 6, h: 3 },
      category: 'chart',
      component: placeholder('Channel Bar'),
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
        { id: 'marketing-leads',     label: 'Leads',     icon: '🧲', path: '/marketing/leads',     order: 32 },
      ],
    },
  ],

  onInit: async () => { console.info('[Marketing Plugin] Initialized'); },
};
