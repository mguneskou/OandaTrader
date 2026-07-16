import type { ReactNode } from 'react'

interface StatTileProps {
  label: string
  value: ReactNode
  delta?: ReactNode
  deltaClass?: string
  hero?: boolean
}

export function StatTile({ label, value, delta, deltaClass = '', hero = false }: StatTileProps) {
  return (
    <div className="card">
      <div className="stat-label">{label}</div>
      <div className={hero ? 'stat-value hero' : 'stat-value'}>{value}</div>
      {delta != null && <div className={`stat-delta ${deltaClass}`}>{delta}</div>}
    </div>
  )
}
