import { type SignalRStatus } from '../hooks/useSignalR';

// ─────────────────────────────────────────────────────────────────────────────
// ConnectionStatusIndicator — coloured dot showing SignalR health
//
// Usage:
//   const { status } = useSignalR();
//   <ConnectionStatusIndicator status={status} />
// ─────────────────────────────────────────────────────────────────────────────

interface ConnectionStatusIndicatorProps {
  status: SignalRStatus;
  /** Show descriptive label next to the dot. Default: true. */
  showLabel?: boolean;
  className?: string;
}

const STATUS_CONFIG: Record<
  SignalRStatus,
  { color: string; pulse: boolean; label: string }
> = {
  connected:     { color: '#22c55e', pulse: false, label: 'Live'         },
  reconnecting:  { color: '#f59e0b', pulse: true,  label: 'Reconnecting' },
  connecting:    { color: '#94a3b8', pulse: true,  label: 'Connecting'   },
  disconnected:  { color: '#ef4444', pulse: false, label: 'Offline'      },
};

export function ConnectionStatusIndicator({
  status,
  showLabel = true,
  className,
}: ConnectionStatusIndicatorProps) {
  const { color, pulse, label } = STATUS_CONFIG[status];

  return (
    <span
      className={className}
      title={`Realtime: ${label}`}
      style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}
    >
      <span
        style={{
          width: 8,
          height: 8,
          borderRadius: '50%',
          backgroundColor: color,
          display: 'inline-block',
          flexShrink: 0,
          animation: pulse ? 'ioc-pulse 1.4s ease-in-out infinite' : 'none',
        }}
      />
      {showLabel && (
        <span
          style={{
            fontSize: 12,
            color: '#64748b',
            lineHeight: 1,
            userSelect: 'none',
          }}
        >
          {label}
        </span>
      )}

      {/* Keyframe injected inline — avoids global CSS dependency */}
      <style>{`
        @keyframes ioc-pulse {
          0%, 100% { opacity: 1; transform: scale(1); }
          50%       { opacity: 0.4; transform: scale(0.85); }
        }
      `}</style>
    </span>
  );
}
