// ─────────────────────────────────────────────────────────────────────────────
// IOC Plugin Registry — Central registry cho tất cả IOC plugins
// ─────────────────────────────────────────────────────────────────────────────

export interface RouteConfig {
  path: string;
  component: React.LazyExoticComponent<React.ComponentType>;
  label: string;
}

export interface WidgetConfig {
  id: string;
  name: string;
  description: string;
  defaultSize: { w: number; h: number };
  component: React.LazyExoticComponent<React.ComponentType<WidgetProps>>;
  category: 'chart' | 'kpi' | 'table' | 'custom';
}

export interface WidgetProps {
  config?: Record<string, unknown>;
  isEditing?: boolean;
}

export interface MenuConfig {
  id: string;
  label: string;
  icon: string;
  path: string;
  order: number;
  children?: MenuConfig[];
}

export interface IOCPlugin {
  id: string;
  name: string;
  version: string;
  description: string;
  icon: string;
  color: string;               // Theme color cho plugin
  routes: RouteConfig[];
  widgets: WidgetConfig[];
  menuItems: MenuConfig[];
  onInit?: () => void | Promise<void>;
  onDestroy?: () => void;
}

// ─────────────────────────────────────────────────────────────────────────────

class IOCPluginRegistry {
  private readonly _plugins = new Map<string, IOCPlugin>();
  private _initialized = false;

  register(plugin: IOCPlugin): void {
    if (this._plugins.has(plugin.id)) {
      console.warn(`[IOC] Plugin "${plugin.id}" đã được đăng ký. Bỏ qua.`);
      return;
    }
    this._plugins.set(plugin.id, plugin);
    console.info(`[IOC] Plugin "${plugin.name}" v${plugin.version} đã đăng ký.`);
  }

  unregister(pluginId: string): void {
    const plugin = this._plugins.get(pluginId);
    plugin?.onDestroy?.();
    this._plugins.delete(pluginId);
  }

  async initAll(): Promise<void> {
    if (this._initialized) return;
    for (const plugin of this._plugins.values()) {
      await plugin.onInit?.();
    }
    this._initialized = true;
  }

  getAll(): IOCPlugin[] {
    return Array.from(this._plugins.values());
  }

  getById(id: string): IOCPlugin | undefined {
    return this._plugins.get(id);
  }

  getAllRoutes(): RouteConfig[] {
    return this.getAll().flatMap((p) => p.routes);
  }

  getAllWidgets(): WidgetConfig[] {
    return this.getAll().flatMap((p) => p.widgets);
  }

  getAllMenuItems(): MenuConfig[] {
    return this.getAll()
      .flatMap((p) => p.menuItems)
      .sort((a, b) => a.order - b.order);
  }
}

// Singleton instance
export const pluginRegistry = new IOCPluginRegistry();
