interface ProgressBarProps {
  percent: number
  label: string
  failed?: boolean
}

export function ProgressBar({ percent, label, failed = false }: ProgressBarProps) {
  const clamped = Math.max(0, Math.min(100, percent))
  return (
    <div style={{ marginTop: '0.75rem' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          fontSize: '0.75rem',
          color: 'var(--text-secondary)',
          marginBottom: '0.25rem',
        }}
      >
        <span>{label}</span>
        <span>{clamped}%</span>
      </div>
      <div
        role="progressbar"
        aria-valuenow={clamped}
        aria-valuemin={0}
        aria-valuemax={100}
        style={{
          height: 8,
          borderRadius: 999,
          background: 'var(--page-plane)',
          border: '1px solid var(--border)',
          overflow: 'hidden',
        }}
      >
        <div
          style={{
            height: '100%',
            width: `${clamped}%`,
            background: failed ? 'var(--status-critical)' : 'var(--series-1)',
            transition: 'width 0.3s ease',
          }}
        />
      </div>
    </div>
  )
}
