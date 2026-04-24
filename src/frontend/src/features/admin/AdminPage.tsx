import { useState } from 'react';
import { ModuleManager } from './ModuleManager';
import { ChartBuilder } from './ChartBuilder';
import { PermissionManager } from './PermissionManager';

type AdminTab = 'modules' | 'charts' | 'permissions';

export function AdminPage() {
  const [tab, setTab] = useState<AdminTab>('modules');
  const [selectedModuleId, setSelectedModuleId] = useState<string | null>(null);

  return (
    <div style={{ padding: '24px 28px', height: '100%', display: 'flex', flexDirection: 'column', gap: 20 }}>
      {/* Page header */}
      <div>
        <h1 style={{ margin: 0, fontSize: 20, fontWeight: 700, color: '#f1f5f9' }}>
          Quan tri he thong
        </h1>
        <p style={{ margin: '4px 0 0', fontSize: 13, color: '#64748b' }}>
          Cau hinh modules, bieu do va phan quyen
        </p>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 4, borderBottom: '1px solid #1e293b', paddingBottom: 0 }}>
        {([
          { key: 'modules',     label: 'Modules' },
          { key: 'charts',      label: 'Bieu do' },
          { key: 'permissions', label: 'Phan quyen' },
        ] as { key: AdminTab; label: string }[]).map(({ key, label }) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            style={{
              padding: '8px 16px',
              border: 'none',
              background: 'none',
              cursor: 'pointer',
              fontSize: 13,
              fontWeight: tab === key ? 600 : 400,
              color: tab === key ? '#0ea5e9' : '#64748b',
              borderBottom: tab === key ? '2px solid #0ea5e9' : '2px solid transparent',
              marginBottom: -1,
              transition: 'all 0.15s',
            }}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      <div style={{ flex: 1, overflow: 'auto' }}>
        {tab === 'modules' && (
          <ModuleManager
            onSelectModule={(id) => { setSelectedModuleId(id); setTab('charts'); }}
          />
        )}
        {tab === 'charts' && (
          <ChartBuilder selectedModuleId={selectedModuleId} />
        )}
        {tab === 'permissions' && (
          <PermissionManager selectedModuleId={selectedModuleId} />
        )}
      </div>
    </div>
  );
}
