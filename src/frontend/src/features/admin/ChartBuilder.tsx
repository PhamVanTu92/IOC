import React, { useState, useMemo } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import {
  GET_MODULES, GET_CHARTS_BY_MODULE,
  CREATE_CHART, UPDATE_CHART, DELETE_CHART,
} from '@/graphql/moduleQueries';
import type { ModuleGql, ChartGql, ChartConfig } from '@/graphql/moduleTypes';
import { ChartRenderer } from '@/features/charts/ChartRenderer';

const CHART_TYPES = [
  { value: 'bar',     label: 'Bar Chart' },
  { value: 'line',    label: 'Line Chart' },
  { value: 'area',    label: 'Area Chart' },
  { value: 'pie',     label: 'Pie Chart' },
  { value: 'kpi',     label: 'KPI Card' },
  { value: 'table',   label: 'Table' },
  { value: 'scatter', label: 'Scatter Plot' },
] as const;

const AGGREGATIONS = ['sum', 'count', 'avg', 'max', 'min'] as const;

// Sample preview data
const SAMPLE_DATA: Record<string, unknown>[] = [
  { month: 'T1', name: 'Thang 1', revenue: 420000, count: 120, value: 420000 },
  { month: 'T2', name: 'Thang 2', revenue: 380000, count: 98,  value: 380000 },
  { month: 'T3', name: 'Thang 3', revenue: 510000, count: 145, value: 510000 },
  { month: 'T4', name: 'Thang 4', revenue: 460000, count: 132, value: 460000 },
  { month: 'T5', name: 'Thang 5', revenue: 620000, count: 178, value: 620000 },
  { month: 'T6', name: 'Thang 6', revenue: 580000, count: 165, value: 580000 },
];

interface Props {
  selectedModuleId?: string | null;
}

type ChartTypeValue = 'bar' | 'line' | 'area' | 'pie' | 'kpi' | 'table' | 'scatter';

interface FormState {
  moduleId: string;
  name: string;
  description: string;
  chartType: ChartTypeValue;
  sortOrder: number;
  config: ChartConfig;
}

const defaultForm: FormState = {
  moduleId: '',
  name: '',
  description: '',
  chartType: 'bar',
  sortOrder: 0,
  config: {
    title: '',
    datasource: '',
    xField: 'month',
    yField: 'revenue',
    aggregation: 'sum',
    colors: ['#0ea5e9'],
    unit: '',
  },
};

export function ChartBuilder({ selectedModuleId }: Props) {
  const { data: modulesData } = useQuery<{ modules: ModuleGql[] }>(GET_MODULES);
  const [moduleId, setModuleId] = useState(selectedModuleId ?? '');

  const { data: chartsData, refetch } = useQuery<{ chartsByModule: ChartGql[] }>(
    GET_CHARTS_BY_MODULE,
    { variables: { moduleId }, skip: !moduleId }
  );

  const [createChart] = useMutation(CREATE_CHART, { onCompleted: () => { void refetch(); setShowForm(false); } });
  const [updateChart] = useMutation(UPDATE_CHART, { onCompleted: () => { void refetch(); setEditing(null); setShowForm(false); } });
  const [deleteChart] = useMutation(DELETE_CHART, { onCompleted: () => void refetch() });

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing]   = useState<ChartGql | null>(null);
  const [form, setForm]         = useState<FormState>(defaultForm);

  function openCreate() {
    setForm({ ...defaultForm, moduleId });
    setEditing(null);
    setShowForm(true);
  }

  function openEdit(c: ChartGql) {
    let config: ChartConfig = {};
    try { config = JSON.parse(c.configJson) as ChartConfig; } catch { /**/ }
    setForm({
      moduleId: c.moduleId,
      name: c.name,
      description: c.description ?? '',
      chartType: c.chartType as ChartTypeValue,
      sortOrder: c.sortOrder,
      config,
    });
    setEditing(c);
    setShowForm(true);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const configJson = JSON.stringify(form.config);
    if (editing) {
      void updateChart({ variables: { input: { id: editing.id, name: form.name, description: form.description, chartType: form.chartType, configJson, sortOrder: form.sortOrder } } });
    } else {
      void createChart({ variables: { input: { moduleId: form.moduleId || moduleId, name: form.name, description: form.description, chartType: form.chartType, configJson, sortOrder: form.sortOrder } } });
    }
  }

  // Preview chart
  const previewChart: ChartGql = useMemo(() => ({
    id: 'preview', moduleId: '', name: form.name || 'Preview',
    description: form.description, chartType: form.chartType,
    configJson: JSON.stringify(form.config), sortOrder: 0, createdAt: '',
  }), [form]);

  const charts = chartsData?.chartsByModule ?? [];
  const modules = modulesData?.modules ?? [];

  return (
    <div style={{ display: 'flex', gap: 24, height: '100%' }}>
      {/* Left: module selector + chart list */}
      <div style={{ width: 280, flexShrink: 0, display: 'flex', flexDirection: 'column', gap: 12 }}>
        {/* Module selector */}
        <div>
          <label style={labelStyle}>Module</label>
          <select value={moduleId} onChange={e => setModuleId(e.target.value)} style={inputStyle}>
            <option value="">-- Chon module --</option>
            {modules.map(m => <option key={m.id} value={m.id}>{m.icon} {m.name}</option>)}
          </select>
        </div>

        {moduleId && (
          <>
            <button onClick={openCreate} style={btnStyle}>+ Them bieu do</button>
            <div style={{ fontSize: 12, color: '#475569', fontWeight: 500 }}>{charts.length} bieu do</div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8, overflowY: 'auto' }}>
              {charts.map(c => (
                <div key={c.id} style={{
                  backgroundColor: '#0f172a', border: '1px solid #1e293b',
                  borderRadius: 8, padding: '10px 12px',
                }}>
                  <div style={{ fontSize: 13, fontWeight: 500, color: '#f1f5f9', marginBottom: 4 }}>{c.name}</div>
                  <div style={{ fontSize: 11, color: '#475569', marginBottom: 8 }}>{c.chartType}</div>
                  <div style={{ display: 'flex', gap: 6 }}>
                    <button onClick={() => openEdit(c)} style={smallBtnStyle}>Sua</button>
                    <button onClick={() => { if (confirm(`Xoa "${c.name}"?`)) void deleteChart({ variables: { id: c.id } }); }}
                      style={{ ...smallBtnStyle, color: '#ef4444' }}>Xoa</button>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>

      {/* Right: form + preview */}
      {showForm && (
        <div style={{ flex: 1, display: 'flex', gap: 20, overflow: 'hidden' }}>
          {/* Form */}
          <div style={{ width: 340, backgroundColor: '#0f172a', border: '1px solid #1e293b', borderRadius: 10, padding: 20, overflowY: 'auto', flexShrink: 0 }}>
            <h3 style={{ margin: '0 0 16px', fontSize: 14, fontWeight: 600, color: '#f1f5f9' }}>
              {editing ? 'Sua bieu do' : 'Tao bieu do moi'}
            </h3>
            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <FormField label="Ten bieu do">
                <input style={inputStyle} value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} required />
              </FormField>
              <FormField label="Loai bieu do">
                <select style={inputStyle} value={form.chartType}
                  onChange={e => setForm(f => ({ ...f, chartType: e.target.value as ChartTypeValue }))}>
                  {CHART_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </FormField>
              <FormField label="Datasource (API/dataset)">
                <input style={inputStyle} placeholder="api/sales hoac dataset-uuid"
                  value={form.config.datasource ?? ''}
                  onChange={e => setForm(f => ({ ...f, config: { ...f.config, datasource: e.target.value } }))} />
              </FormField>

              {form.chartType !== 'kpi' && form.chartType !== 'table' && (
                <>
                  <FormField label="X Axis field">
                    <input style={inputStyle} value={form.config.xField ?? ''}
                      onChange={e => setForm(f => ({ ...f, config: { ...f.config, xField: e.target.value } }))} />
                  </FormField>
                  <FormField label="Y Axis field">
                    <input style={inputStyle} value={form.config.yField ?? ''}
                      onChange={e => setForm(f => ({ ...f, config: { ...f.config, yField: e.target.value } }))} />
                  </FormField>
                  <FormField label="Aggregation">
                    <select style={inputStyle} value={form.config.aggregation ?? 'sum'}
                      onChange={e => setForm(f => ({ ...f, config: { ...f.config, aggregation: e.target.value as 'sum' } }))}>
                      {AGGREGATIONS.map(a => <option key={a} value={a}>{a.toUpperCase()}</option>)}
                    </select>
                  </FormField>
                  <FormField label="Color">
                    <input type="color" value={form.config.colors?.[0] ?? '#0ea5e9'}
                      onChange={e => setForm(f => ({ ...f, config: { ...f.config, colors: [e.target.value] } }))}
                      style={{ ...inputStyle, padding: 4, height: 36, cursor: 'pointer' }} />
                  </FormField>
                </>
              )}

              {form.chartType === 'kpi' && (
                <>
                  <FormField label="Value field">
                    <input style={inputStyle} value={form.config.valueField ?? ''}
                      onChange={e => setForm(f => ({ ...f, config: { ...f.config, valueField: e.target.value } }))} />
                  </FormField>
                  <FormField label="Don vi (VD: d, %)">
                    <input style={inputStyle} value={form.config.unit ?? ''}
                      onChange={e => setForm(f => ({ ...f, config: { ...f.config, unit: e.target.value } }))} />
                  </FormField>
                </>
              )}

              <FormField label="Thu tu">
                <input type="number" style={inputStyle} value={form.sortOrder}
                  onChange={e => setForm(f => ({ ...f, sortOrder: Number(e.target.value) }))} />
              </FormField>

              {/* Raw JSON config (advanced) */}
              <details style={{ marginTop: 4 }}>
                <summary style={{ fontSize: 12, color: '#64748b', cursor: 'pointer' }}>Config JSON (nang cao)</summary>
                <textarea
                  style={{ ...inputStyle, height: 100, marginTop: 6, fontFamily: 'monospace', fontSize: 11, resize: 'vertical' }}
                  value={JSON.stringify(form.config, null, 2)}
                  onChange={e => {
                    try { setForm(f => ({ ...f, config: JSON.parse(e.target.value) as ChartConfig })); }
                    catch { /* ignore parse errors while typing */ }
                  }}
                />
              </details>

              <div style={{ display: 'flex', gap: 8, marginTop: 4 }}>
                <button type="submit" style={btnStyle}>Luu</button>
                <button type="button" onClick={() => setShowForm(false)} style={smallBtnStyle}>Huy</button>
              </div>
            </form>
          </div>

          {/* Live preview */}
          <div style={{ flex: 1, backgroundColor: '#0f172a', border: '1px solid #1e293b', borderRadius: 10, padding: 16, display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div style={{ fontSize: 12, color: '#64748b', fontWeight: 500 }}>Xem truoc (du lieu mau)</div>
            <div style={{ flex: 1 }}>
              <ChartRenderer chart={previewChart} data={SAMPLE_DATA} height={320} />
            </div>
            <details>
              <summary style={{ fontSize: 11, color: '#475569', cursor: 'pointer' }}>JSON Config mau</summary>
              <pre style={{ fontSize: 11, color: '#64748b', marginTop: 8, overflow: 'auto', maxHeight: 160 }}>
                {JSON.stringify(form.config, null, 2)}
              </pre>
            </details>
          </div>
        </div>
      )}
    </div>
  );
}

function FormField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label style={labelStyle}>{label}</label>
      {children}
    </div>
  );
}

const labelStyle: React.CSSProperties = { fontSize: 12, color: '#94a3b8', fontWeight: 500, display: 'block', marginBottom: 4 };
const inputStyle: React.CSSProperties = { width: '100%', boxSizing: 'border-box', backgroundColor: '#0a1628', border: '1px solid #1e293b', borderRadius: 6, color: '#f1f5f9', fontSize: 13, padding: '7px 10px' };
const btnStyle: React.CSSProperties = { padding: '7px 14px', backgroundColor: '#0ea5e9', border: 'none', borderRadius: 6, color: '#fff', fontSize: 13, fontWeight: 600, cursor: 'pointer' };
const smallBtnStyle: React.CSSProperties = { padding: '4px 10px', background: 'none', border: '1px solid #1e293b', borderRadius: 5, color: '#94a3b8', fontSize: 11, cursor: 'pointer' };
