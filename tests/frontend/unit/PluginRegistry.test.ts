import { IOCPluginRegistry, type IOCPlugin } from '../../../src/frontend/src/core/PluginRegistry';

// ─────────────────────────────────────────────────────────────────────────────
// Fake plugin cho tests
// ─────────────────────────────────────────────────────────────────────────────

const createFakePlugin = (id: string, overrides?: Partial<IOCPlugin>): IOCPlugin => ({
  id,
  name: `Plugin ${id}`,
  version: '1.0.0',
  description: 'Test plugin',
  icon: '🧪',
  color: '#000',
  routes: [],
  widgets: [],
  menuItems: [],
  ...overrides,
});

// ─────────────────────────────────────────────────────────────────────────────

describe('IOCPluginRegistry', () => {
  let registry: IOCPluginRegistry;

  beforeEach(() => {
    // Fresh registry cho mỗi test
    registry = new (require('../../../src/frontend/src/core/PluginRegistry').IOCPluginRegistry)();
  });

  describe('register()', () => {
    it('nên đăng ký plugin thành công', () => {
      const plugin = createFakePlugin('finance');
      registry.register(plugin);
      expect(registry.getAll()).toHaveLength(1);
      expect(registry.getById('finance')).toBe(plugin);
    });

    it('nên bỏ qua plugin trùng id', () => {
      const plugin1 = createFakePlugin('finance');
      const plugin2 = createFakePlugin('finance');
      registry.register(plugin1);
      registry.register(plugin2);
      expect(registry.getAll()).toHaveLength(1);
    });

    it('nên hỗ trợ đăng ký nhiều plugins khác nhau', () => {
      registry.register(createFakePlugin('finance'));
      registry.register(createFakePlugin('hr'));
      registry.register(createFakePlugin('marketing'));
      expect(registry.getAll()).toHaveLength(3);
    });
  });

  describe('getAllRoutes()', () => {
    it('nên gom routes từ tất cả plugins', () => {
      registry.register(createFakePlugin('finance', {
        routes: [{ path: '/finance', label: 'Finance', component: null as never }],
      }));
      registry.register(createFakePlugin('hr', {
        routes: [
          { path: '/hr', label: 'HR', component: null as never },
          { path: '/hr/employees', label: 'Employees', component: null as never },
        ],
      }));
      expect(registry.getAllRoutes()).toHaveLength(3);
    });
  });

  describe('getAllMenuItems()', () => {
    it('nên sắp xếp menu theo order', () => {
      registry.register(createFakePlugin('marketing', {
        menuItems: [{ id: 'mkt', label: 'Marketing', icon: '📣', path: '/marketing', order: 30 }],
      }));
      registry.register(createFakePlugin('finance', {
        menuItems: [{ id: 'fin', label: 'Finance', icon: '💰', path: '/finance', order: 10 }],
      }));
      registry.register(createFakePlugin('hr', {
        menuItems: [{ id: 'hr', label: 'HR', icon: '👥', path: '/hr', order: 20 }],
      }));

      const items = registry.getAllMenuItems();
      expect(items[0].id).toBe('fin');   // order 10
      expect(items[1].id).toBe('hr');    // order 20
      expect(items[2].id).toBe('mkt');   // order 30
    });
  });

  describe('getAllWidgets()', () => {
    it('nên gom widgets từ tất cả plugins', () => {
      registry.register(createFakePlugin('finance', {
        widgets: [
          { id: 'finance.kpi', name: 'KPI', description: '', defaultSize: { w: 3, h: 2 }, category: 'kpi', component: null as never },
          { id: 'finance.chart', name: 'Chart', description: '', defaultSize: { w: 6, h: 3 }, category: 'chart', component: null as never },
        ],
      }));
      expect(registry.getAllWidgets()).toHaveLength(2);
    });
  });

  describe('unregister()', () => {
    it('nên xoá plugin khỏi registry', () => {
      registry.register(createFakePlugin('finance'));
      registry.unregister('finance');
      expect(registry.getAll()).toHaveLength(0);
    });
  });
});
