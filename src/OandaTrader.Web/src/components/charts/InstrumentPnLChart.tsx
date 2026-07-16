import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { AnalyticsSummary } from '../../api/client'
import { percent, signedMoney } from '../../lib/format'
import { DivergingBarShape } from './DivergingBarShape'

interface Props {
  data: AnalyticsSummary['perInstrument']
  currency: string
}

interface TooltipPayloadItem {
  payload: AnalyticsSummary['perInstrument'][number]
}

function PnLTooltip({
  active,
  payload,
  currency,
}: {
  active?: boolean
  payload?: TooltipPayloadItem[]
  currency: string
}) {
  if (!active || !payload?.length) return null
  const row = payload[0].payload
  return (
    <div className="viz-tooltip">
      <div className="viz-tooltip-label">{row.instrument}</div>
      <div className="viz-tooltip-value">{signedMoney(row.totalPnL, currency)}</div>
      <div className="viz-tooltip-label" style={{ marginTop: 4, marginBottom: 0 }}>
        {row.trades} trades · {percent(row.winRatePercent)} win rate
      </div>
    </div>
  )
}

export function InstrumentPnLChart({ data, currency }: Props) {
  if (data.length === 0) {
    return <div className="empty">No closed trades yet.</div>
  }

  return (
    // Diverging: position vs the zero baseline carries the sign, color reinforces it.
    <div className="chart-box">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} margin={{ top: 8, right: 12, bottom: 4, left: 4 }}>
          <CartesianGrid stroke="var(--gridline)" strokeWidth={1} vertical={false} />
          <XAxis
            dataKey="instrument"
            stroke="var(--baseline)"
            tick={{ fill: 'var(--text-muted)', fontSize: 11 }}
            tickLine={false}
            height={28}
          />
          <YAxis
            stroke="var(--baseline)"
            tick={{ fill: 'var(--text-muted)', fontSize: 11 }}
            tickLine={false}
            width={64}
            tickFormatter={(v: number) => v.toLocaleString()}
          />
          <Tooltip
            content={<PnLTooltip currency={currency} />}
            cursor={{ fill: 'var(--gridline)', fillOpacity: 0.4 }}
          />
          <ReferenceLine y={0} stroke="var(--baseline)" strokeWidth={1} />
          <Bar dataKey="totalPnL" maxBarSize={24} shape={<DivergingBarShape />} isAnimationActive={false}>
            {data.map((row) => (
              <Cell
                key={row.instrument}
                fill={row.totalPnL >= 0 ? 'var(--series-1)' : 'var(--series-neg)'}
              />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
