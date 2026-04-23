import { useEffect, useRef } from 'react';
import { ChartBuilder } from '@/features/chart-builder/ChartBuilder';
import type { ChartConfig } from '@/features/chart-builder/types';

// ─────────────────────────────────────────────────────────────────────────────
// ChartBuilderModal — full-screen modal overlay wrapping ChartBuilder wizard
// Used both for creating new widgets and editing existing ones
// ─────────────────────────────────────────────────────────────────────────────

interface ChartBuilderModalProps {
  /** When editing an existing widget, provide its current config */
  initialConfig?: Partial<ChartConfig>;
  onSave: (config: ChartConfig) => void;
  onClose: () => void;
}

export function ChartBuilderModal({ initialConfig, onSave, onClose }: ChartBuilderModalProps) {
  // Close on Escape
  const overlayRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', handleKey);
    // Lock body scroll while modal is open
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', handleKey);
      document.body.style.overflow = '';
    };
  }, [onClose]);

  function handleOverlayClick(e: React.MouseEvent<HTMLDivElement>) {
    // Close only when clicking the backdrop (not the modal itself)
    if (e.target === overlayRef.current) onClose();
  }

  return (
    <div
      ref={overlayRef}
      onClick={handleOverlayClick}
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 1000,
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        backdropFilter: 'blur(3px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 24,
      }}
    >
      <div
        style={{
          width: '100%',
          maxWidth: 800,
          height: '85vh',
          maxHeight: 800,
          borderRadius: 14,
          overflow: 'hidden',
          boxShadow: '0 24px 64px rgba(0,0,0,0.6)',
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <ChartBuilder
          initialConfig={initialConfig}
          onSave={(config) => {
            onSave(config);
            onClose();
          }}
          onCancel={onClose}
        />
      </div>
    </div>
  );
}
