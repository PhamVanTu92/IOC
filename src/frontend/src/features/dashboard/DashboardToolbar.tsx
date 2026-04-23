import { useState } from 'react';
import { useDashboardStore } from './useDashboardStore';

// ─────────────────────────────────────────────────────────────────────────────
// DashboardToolbar — top bar: title editing, edit mode toggle, save/discard
// ─────────────────────────────────────────────────────────────────────────────

interface DashboardToolbarProps {
  onSave: () => void;
  onDiscard: () => void;
  /** When provided, shows a delete button. */
  onDelete?: () => void;
  /** When provided, shows a back-to-list button. */
  onBack?: () => void;
  saving?: boolean;
}

export function DashboardToolbar({
  onSave,
  onDiscard,
  onDelete,
  onBack,
  saving = false,
}: DashboardToolbarProps) {
  const dashboard = useDashboardStore((s) => s.dashboard);
  const editMode = useDashboardStore((s) => s.editMode);
  const isDirty = useDashboardStore((s) => s.isDirty);
  const setTitle = useDashboardStore((s) => s.setTitle);
  const toggleEditMode = useDashboardStore((s) => s.toggleEditMode);

  const [editingTitle, setEditingTitle] = useState(false);
  const [titleDraft, setTitleDraft] = useState(dashboard.title);

  function commitTitle() {
    const trimmed = titleDraft.trim();
    if (trimmed) setTitle(trimmed);
    else setTitleDraft(dashboard.title);
    setEditingTitle(false);
  }

  const widgetCount = dashboard.widgets.length;

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '10px 20px',
        backgroundColor: '#0f172a',
        borderBottom: '1px solid #1e293b',
        gap: 12,
        flexShrink: 0,
        flexWrap: 'wrap',
      }}
    >
      {/* Left: title + meta */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flex: 1, minWidth: 0 }}>
        {/* Back to list */}
        {onBack && (
          <button
            onClick={onBack}
            title="Danh sách dashboards"
            style={{
              background: 'none',
              border: 'none',
              color: '#6b7280',
              fontSize: 18,
              cursor: 'pointer',
              padding: '0 4px',
              lineHeight: 1,
            }}
          >
            ←
          </button>
        )}

        {/* Title */}
        {editingTitle ? (
          <input
            autoFocus
            value={titleDraft}
            onChange={(e) => setTitleDraft(e.target.value)}
            onBlur={commitTitle}
            onKeyDown={(e) => {
              if (e.key === 'Enter') commitTitle();
              if (e.key === 'Escape') { setTitleDraft(dashboard.title); setEditingTitle(false); }
            }}
            style={{
              background: 'none',
              border: 'none',
              borderBottom: '1px solid #3b82f6',
              color: '#f9fafb',
              fontSize: 18,
              fontWeight: 700,
              outline: 'none',
              padding: '2px 0',
              minWidth: 160,
              maxWidth: 400,
            }}
          />
        ) : (
          <button
            onClick={() => { setTitleDraft(dashboard.title); setEditingTitle(true); }}
            style={{
              background: 'none',
              border: 'none',
              color: '#f9fafb',
              fontSize: 18,
              fontWeight: 700,
              cursor: 'pointer',
              padding: 0,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              maxWidth: 360,
            }}
            title="Click để đổi tên"
          >
            {dashboard.title}
          </button>
        )}

        {/* Meta badges */}
        <span style={{ color: '#374151', fontSize: 12, whiteSpace: 'nowrap' }}>
          {widgetCount} widget{widgetCount !== 1 ? 's' : ''}
        </span>

        {isDirty && (
          <span
            style={{
              fontSize: 10,
              color: '#f59e0b',
              backgroundColor: '#451a03',
              border: '1px solid #92400e',
              borderRadius: 4,
              padding: '1px 6px',
              fontWeight: 600,
            }}
          >
            Chưa lưu
          </span>
        )}
      </div>

      {/* Right: action buttons */}
      <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexShrink: 0 }}>
        {/* Edit mode toggle */}
        <button
          onClick={toggleEditMode}
          style={{
            padding: '6px 14px',
            borderRadius: 6,
            border: `1px solid ${editMode ? '#3b82f6' : '#374151'}`,
            backgroundColor: editMode ? '#1e3a5f' : 'transparent',
            color: editMode ? '#93c5fd' : '#9ca3af',
            fontSize: 13,
            fontWeight: 600,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: 6,
          }}
        >
          {editMode ? '✅ Chỉnh sửa' : '✏️ Chỉnh sửa'}
        </button>

        {/* Discard */}
        {isDirty && (
          <button
            onClick={onDiscard}
            disabled={saving}
            style={{
              padding: '6px 14px',
              borderRadius: 6,
              border: '1px solid #374151',
              backgroundColor: 'transparent',
              color: '#6b7280',
              fontSize: 13,
              cursor: saving ? 'not-allowed' : 'pointer',
              opacity: saving ? 0.5 : 1,
            }}
          >
            Huỷ thay đổi
          </button>
        )}

        {/* Delete */}
        {onDelete && (
          <button
            onClick={onDelete}
            title="Xoá dashboard"
            style={{
              padding: '6px 12px',
              borderRadius: 6,
              border: '1px solid #7f1d1d',
              backgroundColor: 'transparent',
              color: '#ef4444',
              fontSize: 13,
              cursor: 'pointer',
            }}
          >
            🗑️
          </button>
        )}

        {/* Save */}
        <button
          onClick={onSave}
          disabled={!isDirty || saving}
          style={{
            padding: '6px 16px',
            borderRadius: 6,
            border: 'none',
            backgroundColor: isDirty && !saving ? '#059669' : '#1f2937',
            color: isDirty && !saving ? '#fff' : '#374151',
            fontSize: 13,
            fontWeight: 600,
            cursor: isDirty && !saving ? 'pointer' : 'not-allowed',
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            transition: 'all 0.15s',
          }}
        >
          {saving ? '⏳ Đang lưu...' : '💾 Lưu'}
        </button>
      </div>
    </div>
  );
}
