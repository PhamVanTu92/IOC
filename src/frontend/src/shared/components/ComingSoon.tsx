// ─────────────────────────────────────────────────────────────────────────────
// ComingSoon — placeholder cho các trang/widget chưa implement
// ─────────────────────────────────────────────────────────────────────────────

interface ComingSoonProps {
  name?: string;
}

export function ComingSoon({ name }: ComingSoonProps) {
  return (
    <div style={{
      display: 'flex', flexDirection: 'column',
      alignItems: 'center', justifyContent: 'center',
      height: '100%', minHeight: 200,
      color: '#484f58', gap: 8,
    }}>
      <div style={{ fontSize: 40 }}>🚧</div>
      <div style={{ fontSize: 16, fontWeight: 600, color: '#6e7681' }}>
        {name ?? 'Coming Soon'}
      </div>
      <div style={{ fontSize: 13 }}>Tính năng đang được phát triển</div>
    </div>
  );
}
