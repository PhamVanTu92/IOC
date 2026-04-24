import React, { useState } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { GET_MODULES, CREATE_MODULE, UPDATE_MODULE, DELETE_MODULE } from '@/graphql/moduleQueries';
import type { ModuleGql } from '@/graphql/moduleTypes';

interface Props {
  onSelectModule: (id: string) => void;
}

const ICONS = ['◫', '💰', '👥', '📣', '📦', '🏭', '📈', '🎯', '🔧', '🌐'];
const COLORS = ['#0ea5e9', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6', '#f97316'];

export function ModuleManager({ onSelectModule }: Props) {
  const { data, loading, refetch } = useQuery<{ modules: ModuleGql[] }>(GET_MODULES);
  const [createModule] = useMutation(CREATE_MODULE, { onCompleted: () => { void refetch(); setShowForm(false); } });
  const [updateModule] = useMutation(UPDATE_MODULE, { onCompleted: () => { void refetch(); setEditing(null); } });
  const [deleteModule] = useMutation(DELETE_MODULE, { onCompleted: () => void refetch() });

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing]   = useState<ModuleGql | null>(null);
  const [form, setForm] = useState({ name: '', slug: '', description: '', icon: '◫', color: '#0ea5e9', sortOrder: 0 });

  function openCreate() {
    setForm({ name: '', slug: '', description: '', icon: '◫', color: '#0ea5e9', sortOrder: 0 });
    setEditing(null);
    setShowForm(true);
  }

  function openEdit(m: ModuleGql) {
    setForm({ name: m.name, slug: m.slug, description: m.description ?? '', icon: m.icon, color: m.color, sortOrder: m.sortOrder });
    setEditing(m);
    setShowForm(true);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (editing) {
      void updateModule({ variables: { input: { id: editing.id, name: form.name, description: form.description, icon: form.icon, color: form.color, sortOrder: form.sortOrder } } });
    } else {
      void createModule({ variables: { input: { name: form.name, slug: form.slug, description: form.description, icon: form.icon, color: form.color, sortOrder: form.sortOrder } } });
    }
  }

  const modules = data?.modules ?? [];

  return (
    <div style={{ display: 'flex', gap: 24, height: '100%' }}>
      {/* Module list */}
      <div style={{ flex: 1 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
          <span style={{ fontSize: 14, fontWeight: 600, color: '#94a3b8' }}>
            {modules.length} module{modules.length !== 1 ? 's' : ''}
          </span>
          <button onClick={openCreate} style={btnStyle}>+ Tao module</button>
        </div>

        {loading && <div style={{ color: '#475569', fontSize: 13 }}>Dang tai...</div>}

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 12 }}>
          {modules.map((m) => (
            <div key={m.id} style={{
              backgroundColor: '#0f172a',
              border: '1px solid #1e293b',
              borderRadius: 10,
              padding: '14px 16px',
              borderLeft: `4px solid ${m.color}`,
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 8 }}>
                <span style={{ fontSize: 22 }}>{m.icon}</span>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#f1f5f9' }}>{m.name}</div>
                  <div style={{ fontSize: 11, color: '#475569' }}>{m.slug}</div>
                </div>
              </div>
              {m.description && (
                <p style={{ fontSize: 12, color: '#64748b', margin: '0 0 10px' }}>{m.description}</p>
              )}
              <div style={{ display: 'flex', gap: 8 }}>
                <button onClick={() => onSelectModule(m.id)} style={smallBtnStyle}>Bieu do →</button>
                <button onClick={() => openEdit(m)} style={smallBtnStyle}>Sua</button>
                <button
                  onClick={() => { if (confirm(`Xoa module "${m.name}"?`)) void deleteModule({ variables: { id: m.id } }); }}
                  style={{ ...smallBtnStyle, color: '#ef4444', borderColor: '#ef4444' }}
                >
                  Xoa
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Create/edit form */}
      {showForm && (
        <div style={{ width: 320, backgroundColor: '#0f172a', border: '1px solid #1e293b', borderRadius: 10, padding: 20, flexShrink: 0 }}>
          <h3 style={{ margin: '0 0 16px', fontSize: 14, fontWeight: 600, color: '#f1f5f9' }}>
            {editing ? 'Sua module' : 'Tao module moi'}
          </h3>
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <Field label="Ten">
              <input style={inputStyle} value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} required />
            </Field>
            {!editing && (
              <Field label="Slug (URL)">
                <input style={inputStyle} value={form.slug} placeholder="sales-dashboard"
                  onChange={e => setForm(f => ({ ...f, slug: e.target.value.toLowerCase().replace(/\s+/g, '-') }))} required />
              </Field>
            )}
            <Field label="Mo ta">
              <input style={inputStyle} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            </Field>
            <Field label="Icon">
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                {ICONS.map(ic => (
                  <button key={ic} type="button" onClick={() => setForm(f => ({ ...f, icon: ic }))}
                    style={{ fontSize: 18, padding: 4, border: form.icon === ic ? '2px solid #0ea5e9' : '2px solid transparent', borderRadius: 6, background: 'none', cursor: 'pointer' }}>
                    {ic}
                  </button>
                ))}
              </div>
            </Field>
            <Field label="Mau">
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                {COLORS.map(c => (
                  <button key={c} type="button" onClick={() => setForm(f => ({ ...f, color: c }))}
                    style={{ width: 24, height: 24, borderRadius: '50%', backgroundColor: c, border: form.color === c ? '3px solid #fff' : '3px solid transparent', cursor: 'pointer' }} />
                ))}
              </div>
            </Field>
            <Field label="Thu tu">
              <input style={inputStyle} type="number" value={form.sortOrder}
                onChange={e => setForm(f => ({ ...f, sortOrder: Number(e.target.value) }))} />
            </Field>
            <div style={{ display: 'flex', gap: 8, marginTop: 4 }}>
              <button type="submit" style={btnStyle}>Luu</button>
              <button type="button" onClick={() => setShowForm(false)} style={{ ...smallBtnStyle }}>Huy</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label style={{ fontSize: 12, color: '#94a3b8', fontWeight: 500, display: 'block', marginBottom: 4 }}>{label}</label>
      {children}
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  width: '100%', boxSizing: 'border-box', backgroundColor: '#0a1628', border: '1px solid #1e293b',
  borderRadius: 6, color: '#f1f5f9', fontSize: 13, padding: '7px 10px',
};

const btnStyle: React.CSSProperties = {
  padding: '7px 14px', backgroundColor: '#0ea5e9', border: 'none', borderRadius: 6,
  color: '#fff', fontSize: 13, fontWeight: 600, cursor: 'pointer',
};

const smallBtnStyle: React.CSSProperties = {
  padding: '4px 10px', background: 'none', border: '1px solid #1e293b',
  borderRadius: 5, color: '#94a3b8', fontSize: 11, cursor: 'pointer',
};
