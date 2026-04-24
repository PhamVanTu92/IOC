import { useCallback, useEffect, useState } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { GET_CHARTS_BY_MODULE, GET_MY_LAYOUT, SAVE_LAYOUT } from '@/graphql/moduleQueries';
import type { ChartGql, LayoutItem } from '@/graphql/moduleTypes';
import { ChartCard } from '@/features/charts/ChartCard';
import { useAuthStore } from '@/features/auth/authStore';

interface Props {
  moduleId: string;
  moduleName: string;
}

export function ModuleDashboard({ moduleId, moduleName }: Props) {
  const { user } = useAuthStore();
  const isAdmin = user?.role === 'admin';

  const { data: chartsData, loading: chartsLoading } = useQuery<{ chartsByModule: ChartGql[] }>(
    GET_CHARTS_BY_MODULE,
    { variables: { moduleId } }
  );
  const { data: layoutData } = useQuery<{ myLayout: { layoutJson: string } | null }>(
    GET_MY_LAYOUT,
    { variables: { moduleId } }
  );
  const [saveLayout] = useMutation(SAVE_LAYOUT);

  const charts = chartsData?.chartsByModule ?? [];

  // Build default layout if none saved
  const [layout, setLayout] = useState<LayoutItem[]>([]);

  useEffect(() => {
    if (charts.length === 0) return;
    if (layoutData?.myLayout?.layoutJson) {
      try {
        const saved = JSON.parse(layoutData.myLayout.layoutJson) as LayoutItem[];
        // Merge: add new charts not in saved layout, drop deleted ones
        const ids = new Set(charts.map(c => c.id));
        const filtered = saved.filter(l => ids.has(l.chartId));
        const newCharts = charts.filter(c => !saved.some(l => l.chartId === c.id));
        const merged: LayoutItem[] = [
          ...filtered,
          ...newCharts.map((c, i) => ({ chartId: c.id, x: (filtered.length + i) % 3, y: Math.floor((filtered.length + i) / 3), w: 1, h: 1, visible: true })),
        ];
        setLayout(merged);
        return;
      } catch { /* fall through to default */ }
    }
    // Default: 3-column grid
    setLayout(charts.map((c, i) => ({ chartId: c.id, x: i % 3, y: Math.floor(i / 3), w: 1, h: 1, visible: true })));
  }, [charts, layoutData]);

  const handleToggleVisible = useCallback((chartId: string) => {
    setLayout(prev => prev.map(l => l.chartId === chartId ? { ...l, visible: !l.visible } : l));
  }, []);

  const handleSaveLayout = useCallback(async () => {
    await saveLayout({ variables: { moduleId, layoutJson: JSON.stringify(layout) } });
  }, [layout, moduleId, saveLayout]);

  const chartMap = new Map(charts.map(c => [c.id, c]));
  const visibleLayout = layout.filter(l => l.visible);

  return (
    <div style={{ padding: '20px 24px', height: '100%', display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <h2 style={{ margin: 0, fontSize: 18, fontWeight: 700, color: '#f1f5f9' }}>{moduleName}</h2>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {/* Toggle hidden charts */}
          <details style={{ position: 'relative' }}>
            <summary style={{ cursor: 'pointer', fontSize: 12, color: '#64748b', padding: '4px 10px', border: '1px solid #1e293b', borderRadius: 6, listStyle: 'none', userSelect: 'none' }}>
              Tuy chinh ({layout.filter(l => !l.visible).length} an)
            </summary>
            <div style={{ position: 'absolute', right: 0, top: '100%', marginTop: 6, backgroundColor: '#0f172a', border: '1px solid #1e293b', borderRadius: 8, padding: 12, zIndex: 10, minWidth: 200 }}>
              {layout.map(l => {
                const c = chartMap.get(l.chartId);
                if (!c) return null;
                return (
                  <label key={l.chartId} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12, color: '#94a3b8', marginBottom: 6, cursor: 'pointer' }}>
                    <input type="checkbox" checked={l.visible} onChange={() => handleToggleVisible(l.chartId)} />
                    {c.name}
                  </label>
                );
              })}
            </div>
          </details>
          <button onClick={() => { void handleSaveLayout(); }} style={{
            padding: '5px 12px', backgroundColor: '#0ea5e9', border: 'none', borderRadius: 6,
            color: '#fff', fontSize: 12, fontWeight: 600, cursor: 'pointer',
          }}>
            Luu layout
          </button>
        </div>
      </div>

      {/* Chart grid */}
      {chartsLoading ? (
        <div style={{ color: '#475569', fontSize: 13 }}>Dang tai bieu do...</div>
      ) : charts.length === 0 ? (
        <div style={{ color: '#475569', fontSize: 13 }}>
          Module nay chua co bieu do nao.
          {isAdmin && ' Vao Quan tri → Bieu do de them.'}
        </div>
      ) : (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: 16,
          flex: 1,
          alignContent: 'start',
        }}>
          {visibleLayout.map(l => {
            const chart = chartMap.get(l.chartId);
            if (!chart) return null;
            return (
              <div key={l.chartId} style={{ gridColumn: `span ${Math.min(l.w, 3)}` }}>
                <ChartCard
                  chart={chart}
                  canEdit={isAdmin}
                  data={[]}  // TODO: fetch actual data per chart datasource
                />
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
