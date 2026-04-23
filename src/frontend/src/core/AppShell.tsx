import React, { Suspense, useEffect, useState } from 'react';
import {
  BrowserRouter,
  Routes,
  Route,
  NavLink,
  Navigate,
  useNavigate,
  useParams,
} from 'react-router-dom';
import { pluginRegistry, type IOCPlugin, type RouteConfig } from './PluginRegistry';
import { ConnectionStatusIndicator } from '@/shared/components/ConnectionStatusIndicator';
import { useSignalR } from '@/shared/hooks/useSignalR';

// ─────────────────────────────────────────────────────────────────────────────
// AppShell — root layout with sidebar nav + main content area
//
// Route structure:
//   /                     → redirect to /dashboards
//   /dashboards           → DashboardListPage
//   /dashboards/new       → DashboardPage (blank)
//   /dashboards/:id       → DashboardPage (loaded)
//   /{plugin-routes}      → dynamically registered by plugins
// ─────────────────────────────────────────────────────────────────────────────

// ── Sidebar ───────────────────────────────────────────────────────────────────

function SidebarMenu({ plugins }: { plugins: IOCPlugin[] }) {
  const menuItems = pluginRegistry.getAllMenuItems();

  return (
    <aside className="ioc-sidebar">
      <div className="ioc-sidebar__logo">
        <span className="ioc-logo-icon">⬡</span>
        <span className="ioc-logo-text">IOC</span>
      </div>

      <nav className="ioc-sidebar__nav">
        <NavLink
          to="/dashboards"
          className={({ isActive }) =>
            isActive ? 'ioc-nav-item ioc-nav-item--active' : 'ioc-nav-item'
          }
        >
          <span className="ioc-nav-icon">◫</span>
          <span>Dashboards</span>
        </NavLink>

        {menuItems.map((item) => (
          <NavLink
            key={item.id}
            to={item.path}
            className={({ isActive }) =>
              isActive ? 'ioc-nav-item ioc-nav-item--active' : 'ioc-nav-item'
            }
          >
            <span className="ioc-nav-icon">{item.icon}</span>
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="ioc-sidebar__plugins">
        <p className="ioc-sidebar__section-title">MODULES</p>
        {plugins.map((plugin) => (
          <div
            key={plugin.id}
            className="ioc-plugin-badge"
            style={{ '--plugin-color': plugin.color } as React.CSSProperties}
          >
            <span>{plugin.icon}</span>
            <span>{plugin.name}</span>
          </div>
        ))}
      </div>
    </aside>
  );
}

// ── Top header bar ────────────────────────────────────────────────────────────

function TopBar() {
  const { status } = useSignalR();

  return (
    <div
      style={{
        height: 48,
        borderBottom: '1px solid #1e293b',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'flex-end',
        padding: '0 20px',
        flexShrink: 0,
        backgroundColor: '#0a1628',
      }}
    >
      <ConnectionStatusIndicator status={status} />
    </div>
  );
}

// ── Page adapters — bridge React Router params into component props ─────────

function DashboardListAdapter() {
  const navigate = useNavigate();
  return (
    <Suspense fallback={<PageLoader />}>
      <DashboardListPage
        onOpen={(id) => navigate(`/dashboards/${id}`)}
        onCreateNew={() => navigate('/dashboards/new')}
      />
    </Suspense>
  );
}

function DashboardDetailAdapter() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isNew = id === 'new';

  return (
    <Suspense fallback={<PageLoader />}>
      <DashboardPage
        dashboardId={isNew ? undefined : id}
        onDeleted={() => navigate('/dashboards')}
        onBack={() => navigate('/dashboards')}
      />
    </Suspense>
  );
}

// ── AppShell ──────────────────────────────────────────────────────────────────

export function AppShell() {
  const [ready, setReady] = useState(false);
  const plugins = pluginRegistry.getAll();
  const routes = pluginRegistry.getAllRoutes();

  useEffect(() => {
    pluginRegistry.initAll().then(() => setReady(true));
  }, []);

  if (!ready) {
    return (
      <div className="ioc-loading">
        <div className="ioc-spinner" />
        <p>Đang khởi tạo IOC...</p>
      </div>
    );
  }

  return (
    <BrowserRouter>
      <div className="ioc-layout">
        <SidebarMenu plugins={plugins} />

        <div style={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}>
          <TopBar />

          <main className="ioc-main" style={{ flex: 1, overflow: 'auto' }}>
            <Suspense fallback={<PageLoader />}>
              <Routes>
                <Route path="/" element={<Navigate to="/dashboards" replace />} />

                {/* Dashboard routes */}
                <Route path="/dashboards" element={<DashboardListAdapter />} />
                <Route path="/dashboards/:id" element={<DashboardDetailAdapter />} />

                {/* Plugin-registered routes */}
                {routes.map((route: RouteConfig) => (
                  <Route
                    key={route.path}
                    path={route.path}
                    element={
                      <Suspense fallback={<PageLoader />}>
                        <route.component />
                      </Suspense>
                    }
                  />
                ))}

                <Route path="*" element={<NotFound />} />
              </Routes>
            </Suspense>
          </main>
        </div>
      </div>
    </BrowserRouter>
  );
}

// ── Lazy page imports ─────────────────────────────────────────────────────────

const DashboardListPage = React.lazy(() =>
  import('@/features/dashboard/DashboardListPage').then((m) => ({
    default: m.DashboardListPage,
  }))
);

const DashboardPage = React.lazy(() =>
  import('@/features/dashboard/DashboardPage').then((m) => ({
    default: m.DashboardPage,
  }))
);

// ── Shared small components ───────────────────────────────────────────────────

function PageLoader() {
  return (
    <div
      style={{
        height: '100%',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        color: '#4b5563',
        fontSize: 13,
      }}
    >
      Đang tải...
    </div>
  );
}

function NotFound() {
  return (
    <div
      style={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        color: '#4b5563',
      }}
    >
      <div style={{ fontSize: 48, marginBottom: 16 }}>404</div>
      <div>Trang không tồn tại</div>
    </div>
  );
}
