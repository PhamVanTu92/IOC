import { useState } from 'react';
import { useDashboardList, useDashboardSave, listLocalDashboards } from './useDashboardPersistence';
import { useDashboardStore } from './useDashboardStore';
import type { DashboardSummaryGql } from '@/graphql/types';

// ─────────────────────────────────────────────────────────────────────────────
// DashboardListPage — gallery of saved dashboards
// ─────────────────────────────────────────────────────────────────────────────

interface DashboardListPageProps {
  onOpen: (id: string) => void;
  onCreateNew: () => void;
}

export function DashboardListPage({ onOpen, onCreateNew }: DashboardListPageProps) {
  const { summaries, loading, error, refetch } = useDashboardList();
  const { remove, deleting } = useDashboardSave();
  const resetDashboard = useDashboardStore((s) => s.resetDashboard);

  const [deletingId, setDeletingId] = useState<string | null>(null);

  async function handleDelete(id: string, title: string) {
    if (!window.confirm(`Xoá dashboard "${title}"?`)) return;
    setDeletingId(id);
    try {
      await remove(id);
      refetch();
    } finally {
      setDeletingId(null);
    }
  }

  function handleCreateNew() {
    resetDashboard();
    onCreateNew();
  }

  return (
    <div style={{ minHeight: '100vh', backgroundColor: '#060d1a', padding: 32 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 32 }}>
        <div>
          <h1 style={{ color: '#f9fafb', fontSize: 24, fontWeight: 700, margin: 0 }}>
            Dashboards
          </h1>
          <p style={{ color: '#4b5563', fontSize: 13, margin: '4px 0 0' }}>
            Quản lý các dashboard điều hành của bạn
          </p>
        </div>
        <button
          onClick={handleCreateNew}
          style={{
            padding: '10px 20px',
            borderRadius: 8,
            border: 'none',
            backgroundColor: '#2563eb',
            color: '#fff',
            fontSize: 14,
            fontWeight: 600,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: 8,
          }}
        >
          + Dashboard mới
        </button>
      </div>

      {/* Error */}
      {error && (
        <div style={{ color: '#fca5a5', marginBottom: 16, fontSize: 13 }}>
          Lỗi tải dữ liệu: {error}
        </div>
      )}

      {/* Loading */}
      {loading && summaries.length === 0 && (
        <div style={{ color: '#4b5563', textAlign: 'center', padding: 48 }}>
          Đang tải...
        </div>
      )}

      {/* Empty state */}
      {!loading && summaries.length === 0 && !error && (
        <div
          style={{
            textAlign: 'center',
            padding: 64,
            border: '2px dashed #1e293b',
            borderRadius: 12,
          }}
        >
          <div style={{ fontSize: 48, marginBottom: 16 }}>📊</div>
          <div style={{ color: '#4b5563', fontSize: 16 }}>Chưa có dashboard nào</div>
          <button
            onClick={handleCreateNew}
            style={{
              marginTop: 16,
              padding: '8px 20px',
              borderRadius: 6,
              border: '1px solid #374151',
              backgroundColor: 'transparent',
              color: '#9ca3af',
              cursor: 'pointer',
              fontSize: 13,
            }}
          >
            Tạo dashboard đầu tiên
          </button>
        </div>
      )}

      {/* Grid */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
          gap: 16,
        }}
      >
        {summaries.map((s) => (
          <DashboardCard
            key={s.id}
            summary={s}
            isDeleting={deletingId === s.id || deleting}
            onOpen={() => onOpen(s.id)}
            onDelete={() => handleDelete(s.id, s.title)}
          />
        ))}
      </div>
    </div>
  );
}

// ── DashboardCard ─────────────────────────────────────────────────────────────

interface DashboardCardProps {
  summary: DashboardSummaryGql;
  isDeleting: boolean;
  onOpen: () => void;
  onDelete: () => void;
}

function DashboardCard({ summary, isDeleting, onOpen, onDelete }: DashboardCardProps) {
  const [hovered, setHovered] = useState(false);

  const updatedAgo = formatRelative(summary.updatedAt);

  return (
    <div
      style={{
        borderRadius: 10,
        border: `1px solid ${hovered ? '#374151' : '#1e293b'}`,
        backgroundColor: '#0f172a',
        overflow: 'hidden',
        cursor: 'pointer',
        transition: 'border-color 0.15s',
        position: 'relative',
        opacity: isDeleting ? 0.5 : 1,
      }}
      onClick={onOpen}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      {/* Preview area */}
      <div
        style={{
          height: 120,
          backgroundColor: '#111827',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 32,
        }}
      >
        📊
      </div>

      {/* Body */}
      <div style={{ padding: '12px 14px' }}>
        <div style={{ color: '#f9fafb', fontWeight: 600, fontSize: 14, marginBottom: 4 }}>
          {summary.title}
        </div>
        {summary.description && (
          <div style={{ color: '#4b5563', fontSize: 12, marginBottom: 8 }}>
            {summary.description}
          </div>
        )}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div style={{ color: '#374151', fontSize: 11 }}>
            {summary.widgetCount} widget{summary.widgetCount !== 1 ? 's' : ''} · {updatedAgo}
          </div>
          <button
            onClick={(e) => { e.stopPropagation(); onDelete(); }}
            style={{
              background: 'none',
              border: 'none',
              color: hovered ? '#ef4444' : '#374151',
              cursor: 'pointer',
              fontSize: 13,
              padding: '2px 4px',
              borderRadius: 4,
              transition: 'color 0.1s',
            }}
            title="Xoá dashboard"
          >
            🗑️
          </button>
        </div>
      </div>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatRelative(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return 'vừa xong';
  if (mins < 60) return `${mins} phút trước`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days} ngày trước`;
  return new Date(iso).toLocaleDateString('vi-VN');
}
