import React, { Suspense, useEffect, useState } from 'react';
import {
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
import { useAuthStore } from '@/features/auth/authStore';

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
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <div
      style={{
        height: 48,
        borderBottom: '1px solid #1e293b',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'flex-end',
        gap: 16,
        padding: '0 20px',
        flexShrink: 0,
        backgroundColor: '#0a1628',
      }}
    >
      <ConnectionStatusIndicator status={status} />

      {user && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: 13, color: '#f1f5f9', fontWeight: 500 }}>
              {user.fullName}
            </div>
            <div style={{ fontSize: 11, color: '#64748b' }}>{user.email}</div>
          </div>

          <div
            style={{
              width: 32,
              height: 32,
              borderRadius: '50%',
              backgroundColor: '#0ea5e9',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: 13,
              fontWeight: 700,
              color: '#fff',
              flexShrink: 0,
            }}
          >
            {user.fullName.charAt(0).toUpperCase()}
          </div>

          <button
            onClick={handleLogout}
            style={{
              background: 'none',
              border: '1px solid #1e293b',
              borderRadius: 6,
              color: '#94a3b8',
              cursor: 'pointer',
              fontSize: 12,
              padding: '4px 10px',
              transition: 'color 0.15s, border-color 0.15s',
            }}
            title="Đăng xuất"
          >
            Đăng xuất
          </button>
        </div>
      )}
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
        onSaved={(newId) => navigate(`/dashboards/${newId}`, { replace: true })}
      />
    </Suspense>
  );
}

// ── AppShell ──────────────────────────────────────────────────────────────────

export function AppShell() {
  const [ready, setReady] = useState(false);
  const plugins = pluginRegistry.getAll();
  const routes = pluginRegistry.getAllRoutes();

  // Auth guard — all hooks must be called before any conditional return
  const { isAuthenticated } = useAuthStore();

  useEffect(() => {
    if (isAuthenticated) {
      pluginRegistry.initAll().then(() => setReady(true));
    }
  }, [isAuthenticated]);

  // Redirect unauthenticated users to /login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!ready) {
    return (
      <div className="ioc-loading">
        <div className="ioc-spinner" />
        <p>Đang khởi tạo IOC...</p>
      </div>
    );
  }

  return (
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
