import { useState, useCallback, useEffect } from 'react';
import { DashboardGrid } from './DashboardGrid';
import { DashboardToolbar } from './DashboardToolbar';
import { WidgetLibrary } from './WidgetLibrary';
import { ChartBuilderModal } from './ChartBuilderModal';
import { useDashboardStore } from './useDashboardStore';
import { useDashboardSave, useDashboardLoad } from './useDashboardPersistence';
import { useDashboardRealtime } from './useDashboardRealtime';
import type { ChartConfig } from '@/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// DashboardPage — top-level composition
//
//  ┌─────────────────────────────────────────────────┐
//  │  DashboardToolbar (title, edit toggle, save)    │
//  ├────────────┬────────────────────────────────────┤
//  │  Widget    │  DashboardGrid                     │
//  │  Library   │  (12-col dnd-kit canvas)           │
//  │  (edit     │                                    │
//  │   mode)    │                                    │
//  └────────────┴────────────────────────────────────┘
//     [ChartBuilderModal — portal overlay]
// ─────────────────────────────────────────────────────────────────────────────

interface DashboardPageProps {
  /** When provided, loads an existing dashboard from the backend. */
  dashboardId?: string;
  /** Called when this dashboard is deleted remotely or by the user. */
  onDeleted?: () => void;
  /** Called when the user clicks "back to list" in the toolbar. */
  onBack?: () => void;
}

type ModalState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; widgetId: string };

export function DashboardPage({ dashboardId, onDeleted, onBack }: DashboardPageProps) {
  const editMode          = useDashboardStore((s) => s.editMode);
  const isDirty           = useDashboardStore((s) => s.isDirty);
  const dashboard         = useDashboardStore((s) => s.dashboard);
  const updateWidgetConfig = useDashboardStore((s) => s.updateWidgetConfig);
  const addWidget         = useDashboardStore((s) => s.addWidget);
  const openEditor        = useDashboardStore((s) => s.openEditor);
  const closeEditor       = useDashboardStore((s) => s.closeEditor);
  const markSaved         = useDashboardStore((s) => s.markSaved);
  const loadDashboard     = useDashboardStore((s) => s.loadDashboard);
  const resetDashboard    = useDashboardStore((s) => s.resetDashboard);

  // ── Remote persistence ─────────────────────────────────────────────────────
  const { config: remoteConfig, loading: loadingRemote } = useDashboardLoad(dashboardId);
  const { save, remove, saving, error: saveError } = useDashboardSave();

  useEffect(() => {
    if (remoteConfig) {
      loadDashboard(remoteConfig);
    } else if (!dashboardId && !loadingRemote) {
      resetDashboard();
    }
  }, [remoteConfig, dashboardId, loadingRemote, loadDashboard, resetDashboard]);

  // ── Realtime — concurrent editor awareness ─────────────────────────────────
  const [concurrentEditBanner, setConcurrentEditBanner] = useState<string | null>(null);

  useDashboardRealtime({
    dashboardId,
    onUpdated: (payload) => {
      // Another user saved — show non-intrusive banner (don't overwrite local edits)
      setConcurrentEditBanner(
        `"${payload.savedBy}" đã lưu dashboard này lúc ${new Date(payload.savedAt).toLocaleTimeString('vi-VN')}`,
      );
    },
    onDeleted: () => {
      onDeleted?.();
    },
  });

  // ── Modal ──────────────────────────────────────────────────────────────────
  const [modal, setModal] = useState<ModalState>({ mode: 'closed' });
  const [saveToastError, setSaveToastError] = useState<string | null>(null);

  const handleEditWidget = useCallback(
    (widgetId: string) => {
      openEditor(widgetId);
      setModal({ mode: 'edit', widgetId });
    },
    [openEditor],
  );

  const handleOpenBuilder = useCallback(() => {
    setModal({ mode: 'create' });
  }, []);

  const handleCloseModal = useCallback(() => {
    closeEditor();
    setModal({ mode: 'closed' });
  }, [closeEditor]);

  const handleSaveFromBuilder = useCallback(
    (config: ChartConfig) => {
      if (modal.mode === 'edit') {
        updateWidgetConfig(modal.widgetId, config);
      } else {
        addWidget(config);
      }
      handleCloseModal();
    },
    [modal, updateWidgetConfig, addWidget, handleCloseModal],
  );

  // ── Dashboard CRUD ─────────────────────────────────────────────────────────

  async function handleSave() {
    if (!isDirty) return;
    setSaveToastError(null);
    try {
      const saved = await save(dashboard);
      if (saved.id !== dashboard.id) {
        loadDashboard(saved);
      } else {
        markSaved();
      }
    } catch (err) {
      setSaveToastError(err instanceof Error ? err.message : 'Lỗi lưu dashboard');
    }
  }

  async function handleDiscard() {
    if (!window.confirm('Huỷ tất cả thay đổi chưa lưu?')) return;
    if (dashboardId && remoteConfig) {
      loadDashboard(remoteConfig);
    } else {
      resetDashboard();
    }
  }

  async function handleDelete() {
    if (!dashboardId) return;
    if (!window.confirm(`Xoá dashboard "${dashboard.title}" vĩnh viễn?`)) return;
    await remove(dashboardId);
    onDeleted?.();
  }

  const editingConfig =
    modal.mode === 'edit'
      ? dashboard.widgets.find((w) => w.id === modal.widgetId)?.chartConfig
      : undefined;

  // ── Loading ────────────────────────────────────────────────────────────────
  if (dashboardId && loadingRemote) {
    return (
      <div style={pageStyle}>
        <div
          style={{
            flex: 1,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#4b5563',
          }}
        >
          Đang tải dashboard...
        </div>
      </div>
    );
  }

  return (
    <div style={pageStyle}>
      {/* Toolbar */}
      <DashboardToolbar
        onSave={handleSave}
        onDiscard={handleDiscard}
        onDelete={dashboardId ? handleDelete : undefined}
        onBack={onBack}
        saving={saving}
      />

      {/* Concurrent-edit banner */}
      {concurrentEditBanner && (
        <div style={infoBannerStyle}>
          👤 {concurrentEditBanner}
          <button
            onClick={() => setConcurrentEditBanner(null)}
            style={{
              marginLeft: 12,
              background: 'none',
              border: 'none',
              color: '#93c5fd',
              cursor: 'pointer',
            }}
          >
            ✕
          </button>
        </div>
      )}

      {/* Save error toast */}
      {(saveToastError ?? saveError) && (
        <div style={errorBannerStyle}>
          ⚠️ {saveToastError ?? saveError}
          <button
            onClick={() => setSaveToastError(null)}
            style={{ marginLeft: 12, background: 'none', border: 'none', color: '#fca5a5', cursor: 'pointer' }}
          >
            ✕
          </button>
        </div>
      )}

      {/* Main content */}
      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {editMode && (
          <div
            style={{
              width: 200,
              flexShrink: 0,
              borderRight: '1px solid #1e293b',
              overflowY: 'auto',
            }}
          >
            <WidgetLibrary onOpenBuilder={handleOpenBuilder} />
          </div>
        )}

        <div style={{ flex: 1, overflowY: 'auto', padding: 20 }}>
          <DashboardGrid onEditWidget={handleEditWidget} />
        </div>
      </div>

      {/* Chart builder overlay */}
      {modal.mode !== 'closed' && (
        <ChartBuilderModal
          initialConfig={editingConfig}
          onSave={handleSaveFromBuilder}
          onClose={handleCloseModal}
        />
      )}
    </div>
  );
}

// ── Styles ────────────────────────────────────────────────────────────────────

const pageStyle: React.CSSProperties = {
  display: 'flex',
  flexDirection: 'column',
  height: '100%',
  backgroundColor: '#060d1a',
  overflow: 'hidden',
};

const errorBannerStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  padding: '8px 20px',
  backgroundColor: '#450a0a',
  borderBottom: '1px solid #991b1b',
  color: '#fca5a5',
  fontSize: 13,
  flexShrink: 0,
};

const infoBannerStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  padding: '8px 20px',
  backgroundColor: '#1e3a5f',
  borderBottom: '1px solid #1d4ed8',
  color: '#93c5fd',
  fontSize: 13,
  flexShrink: 0,
};
