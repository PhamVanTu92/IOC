import { useChartConfig } from './useChartConfig';
import { DatasetSelector } from './DatasetSelector';
import { FieldPicker } from './FieldPicker';
import { ChartTypePicker } from './ChartTypePicker';
import { ChartPreview } from './ChartPreview';
import { isConfigValid, type ChartConfig, type BuilderStep } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// ChartBuilder — 4-step wizard: Dataset → Fields → ChartType → Preview
// ─────────────────────────────────────────────────────────────────────────────

interface ChartBuilderProps {
  initialConfig?: Partial<ChartConfig>;
  onSave?: (config: ChartConfig) => void;
  onCancel?: () => void;
}

const STEPS: { id: BuilderStep; label: string; icon: string }[] = [
  { id: 'dataset', label: 'Dataset', icon: '🗄️' },
  { id: 'fields', label: 'Fields', icon: '📋' },
  { id: 'chartType', label: 'Chart Type', icon: '📊' },
  { id: 'preview', label: 'Preview', icon: '👁️' },
];

export function ChartBuilder({ initialConfig, onSave, onCancel }: ChartBuilderProps) {
  const {
    state,
    goToStep,
    nextStep,
    prevStep,
    setDataset,
    setTitle,
    setChartType,
    toggleDimension,
    toggleMeasure,
    toggleMetric,
    setTimeDimension,
    setGranularity,
    setLimit,
    setVisualOptions,
    reset,
  } = useChartConfig(initialConfig);

  const { step, config, isDirty } = state;
  const currentStepIdx = STEPS.findIndex((s) => s.id === step);
  const isConfigReady = isConfigValid(config);

  function canProceed(): boolean {
    if (step === 'dataset') return !!config.datasetId && !!config.title.trim();
    if (step === 'fields') {
      return config.dimensions.length + config.measures.length + config.metrics.length > 0;
    }
    if (step === 'chartType') return true;
    return true;
  }

  function handleSave() {
    if (onSave && isConfigReady) onSave(config);
  }

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        backgroundColor: '#0f172a',
        borderRadius: 12,
        overflow: 'hidden',
        border: '1px solid #1e293b',
      }}
    >
      {/* Header */}
      <div
        style={{
          padding: '16px 24px',
          borderBottom: '1px solid #1e293b',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          backgroundColor: '#111827',
          flexShrink: 0,
        }}
      >
        <div>
          <h2 style={{ margin: 0, color: '#f9fafb', fontSize: 16, fontWeight: 700 }}>
            Chart Builder
          </h2>
          {config.title && (
            <p style={{ margin: '2px 0 0', color: '#6b7280', fontSize: 12 }}>
              {config.title}
            </p>
          )}
        </div>

        <div style={{ display: 'flex', gap: 8 }}>
          {isDirty && (
            <button
              onClick={() => reset(initialConfig)}
              style={ghostBtnStyle}
            >
              Reset
            </button>
          )}
          {onCancel && (
            <button onClick={onCancel} style={ghostBtnStyle}>
              Huỷ
            </button>
          )}
        </div>
      </div>

      {/* Step tabs */}
      <div
        style={{
          display: 'flex',
          borderBottom: '1px solid #1e293b',
          backgroundColor: '#111827',
          flexShrink: 0,
        }}
      >
        {STEPS.map((s, idx) => {
          const isActive = s.id === step;
          const isPast = idx < currentStepIdx;
          const isReachable = idx <= currentStepIdx || (idx === currentStepIdx + 1 && canProceed());

          return (
            <button
              key={s.id}
              onClick={() => isReachable && goToStep(s.id)}
              disabled={!isReachable}
              style={{
                flex: 1,
                padding: '10px 8px',
                border: 'none',
                borderBottom: `2px solid ${isActive ? '#3b82f6' : 'transparent'}`,
                backgroundColor: 'transparent',
                color: isActive ? '#93c5fd' : isPast ? '#6b7280' : '#374151',
                cursor: isReachable ? 'pointer' : 'default',
                fontSize: 12,
                fontWeight: isActive ? 700 : 500,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: 3,
                transition: 'all 0.15s',
              }}
            >
              <span style={{ fontSize: 16 }}>{s.icon}</span>
              <span>
                {idx + 1}. {s.label}
              </span>
              {isPast && <span style={{ fontSize: 10, color: '#22c55e' }}>✓</span>}
            </button>
          );
        })}
      </div>

      {/* Step content */}
      <div style={{ flex: 1, overflowY: 'auto', padding: 24 }}>
        {step === 'dataset' && (
          <DatasetSelector
            selectedId={config.datasetId}
            onSelect={setDataset}
            title={config.title}
            onTitleChange={setTitle}
          />
        )}

        {step === 'fields' && (
          <FieldPicker
            config={config}
            onToggleDimension={toggleDimension}
            onToggleMeasure={toggleMeasure}
            onToggleMetric={toggleMetric}
            onSetTimeDimension={setTimeDimension}
            onSetGranularity={setGranularity}
            onSetLimit={setLimit}
          />
        )}

        {step === 'chartType' && (
          <ChartTypePicker config={config} onSelect={setChartType} />
        )}

        {step === 'preview' && (
          <ChartPreview
            config={config}
            onVisualOptionChange={(key, value) =>
              setVisualOptions({ [key]: value })
            }
          />
        )}
      </div>

      {/* Footer navigation */}
      <div
        style={{
          padding: '16px 24px',
          borderTop: '1px solid #1e293b',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          backgroundColor: '#111827',
          flexShrink: 0,
        }}
      >
        {/* Back */}
        <button
          onClick={prevStep}
          disabled={currentStepIdx === 0}
          style={currentStepIdx === 0 ? { ...ghostBtnStyle, opacity: 0.35, cursor: 'not-allowed' } : ghostBtnStyle}
        >
          ← Quay lại
        </button>

        <div style={{ display: 'flex', gap: 8 }}>
          {/* Next / Save */}
          {step !== 'preview' ? (
            <button
              onClick={nextStep}
              disabled={!canProceed()}
              style={!canProceed() ? { ...primaryBtnStyle, opacity: 0.45, cursor: 'not-allowed' } : primaryBtnStyle}
            >
              Tiếp theo →
            </button>
          ) : (
            <button
              onClick={handleSave}
              disabled={!isConfigReady || !onSave}
              style={
                !isConfigReady || !onSave
                  ? { ...saveBtnStyle, opacity: 0.45, cursor: 'not-allowed' }
                  : saveBtnStyle
              }
            >
              💾 Lưu biểu đồ
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Button styles ─────────────────────────────────────────────────────────────

const ghostBtnStyle: React.CSSProperties = {
  padding: '8px 16px',
  borderRadius: 6,
  border: '1px solid #374151',
  backgroundColor: 'transparent',
  color: '#9ca3af',
  fontSize: 13,
  cursor: 'pointer',
};

const primaryBtnStyle: React.CSSProperties = {
  padding: '8px 20px',
  borderRadius: 6,
  border: 'none',
  backgroundColor: '#2563eb',
  color: '#fff',
  fontSize: 13,
  fontWeight: 600,
  cursor: 'pointer',
};

const saveBtnStyle: React.CSSProperties = {
  padding: '8px 20px',
  borderRadius: 6,
  border: 'none',
  backgroundColor: '#059669',
  color: '#fff',
  fontSize: 13,
  fontWeight: 600,
  cursor: 'pointer',
};
