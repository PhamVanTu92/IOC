import React, { useState } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { GET_MODULES, GET_MODULE_PERMISSIONS, ASSIGN_MODULE_PERMISSION, REVOKE_MODULE_PERMISSION } from '@/graphql/moduleQueries';
import type { ModuleGql, ModulePermissionGql } from '@/graphql/moduleTypes';

interface Props {
  selectedModuleId?: string | null;
}

export function PermissionManager({ selectedModuleId }: Props) {
  const { data: modulesData } = useQuery<{ modules: ModuleGql[] }>(GET_MODULES);
  const [moduleId, setModuleId] = useState(selectedModuleId ?? '');

  const { data: permData, refetch } = useQuery<{ modulePermissions: ModulePermissionGql[] }>(
    GET_MODULE_PERMISSIONS,
    { variables: { moduleId }, skip: !moduleId }
  );

  const [assignPermission] = useMutation(ASSIGN_MODULE_PERMISSION, { onCompleted: () => void refetch() });
  const [revokePermission] = useMutation(REVOKE_MODULE_PERMISSION, { onCompleted: () => void refetch() });

  const [userId, setUserId]   = useState('');
  const [canView, setCanView] = useState(true);
  const [canEdit, setCanEdit] = useState(false);

  function handleAssign(e: React.FormEvent) {
    e.preventDefault();
    if (!userId || !moduleId) return;
    void assignPermission({ variables: { input: { userId, moduleId, canView, canEdit } } });
    setUserId('');
  }

  const modules = modulesData?.modules ?? [];
  const perms   = permData?.modulePermissions ?? [];

  return (
    <div style={{ display: 'flex', gap: 24 }}>
      <div style={{ width: 300, flexShrink: 0 }}>
        <label style={labelStyle}>Module</label>
        <select value={moduleId} onChange={e => setModuleId(e.target.value)} style={inputStyle}>
          <option value="">-- Chon module --</option>
          {modules.map(m => <option key={m.id} value={m.id}>{m.icon} {m.name}</option>)}
        </select>

        {moduleId && (
          <form onSubmit={handleAssign} style={{ marginTop: 20, display: 'flex', flexDirection: 'column', gap: 10 }}>
            <h4 style={{ margin: 0, fontSize: 13, fontWeight: 600, color: '#94a3b8' }}>Cap quyen</h4>
            <div>
              <label style={labelStyle}>User ID</label>
              <input style={inputStyle} value={userId} onChange={e => setUserId(e.target.value)}
                placeholder="UUID cua user" required />
            </div>
            <div style={{ display: 'flex', gap: 12 }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13, color: '#94a3b8', cursor: 'pointer' }}>
                <input type="checkbox" checked={canView} onChange={e => setCanView(e.target.checked)} />
                Xem
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13, color: '#94a3b8', cursor: 'pointer' }}>
                <input type="checkbox" checked={canEdit} onChange={e => setCanEdit(e.target.checked)} />
                Sua
              </label>
            </div>
            <button type="submit" style={btnStyle}>Cap quyen</button>
          </form>
        )}
      </div>

      {moduleId && (
        <div style={{ flex: 1 }}>
          <h4 style={{ margin: '0 0 12px', fontSize: 13, fontWeight: 600, color: '#94a3b8' }}>
            Danh sach quyen ({perms.length})
          </h4>
          {perms.length === 0 ? (
            <div style={{ color: '#475569', fontSize: 13 }}>Chua co user nao duoc cap quyen</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {perms.map(p => (
                <div key={p.userId} style={{
                  backgroundColor: '#0f172a', border: '1px solid #1e293b',
                  borderRadius: 8, padding: '10px 14px',
                  display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                }}>
                  <div>
                    <div style={{ fontSize: 12, color: '#f1f5f9', fontFamily: 'monospace' }}>{p.userId}</div>
                    <div style={{ fontSize: 11, color: '#475569', marginTop: 2 }}>
                      {p.canView ? '✓ Xem' : '✗ Xem'}
                      {' · '}
                      {p.canEdit ? '✓ Sua' : '✗ Sua'}
                    </div>
                  </div>
                  <button
                    onClick={() => void revokePermission({ variables: { userId: p.userId, moduleId } })}
                    style={{ padding: '4px 10px', background: 'none', border: '1px solid #ef4444', borderRadius: 5, color: '#ef4444', fontSize: 11, cursor: 'pointer' }}
                  >
                    Thu hoi
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

const labelStyle: React.CSSProperties = { fontSize: 12, color: '#94a3b8', fontWeight: 500, display: 'block', marginBottom: 4 };
const inputStyle: React.CSSProperties = { width: '100%', boxSizing: 'border-box', backgroundColor: '#0a1628', border: '1px solid #1e293b', borderRadius: 6, color: '#f1f5f9', fontSize: 13, padding: '7px 10px' };
const btnStyle: React.CSSProperties = { padding: '7px 14px', backgroundColor: '#0ea5e9', border: 'none', borderRadius: 6, color: '#fff', fontSize: 13, fontWeight: 600, cursor: 'pointer', width: '100%' };
