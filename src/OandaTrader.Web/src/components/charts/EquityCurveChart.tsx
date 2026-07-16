import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { AnalyticsSummary } from '../../api/client'
import { dateTime, signedMoney } from '../../lib/format'

interface Props {
  data: AnalyticsSummary['equityCurve']
  currency: string
}

interface TooltipPayloadItem {
  payload: AnalyticsSummary['equityCurve'][number]
}

function EquityTooltip({
  active,
  payload,
  currency,
}: {
  active?: boolean
  payload?: TooltipPayloadItem[]
  currency: string
}) {
  if (!active || !payload?.length) return null
  const point = payload[0].payload
  return (
    <div className="viz-tooltip">
      <div className="viz-tooltip-label">
        Trade {point.index} · {dateTime(point.time)}
      </div>
      <div className="viz-tooltip-value">
        {signedMoney(point.cumulativePnL, currency)} cumulative
      </div>
    </div>
  )
}

export function EquityCurveChart({ data, currency }: Props) {
  if (data.length === 0) {
    return <div className="empty">No closed trades yet — the curve appears once trades resolve.</div>
  }

  return (
    // Single series, so no legend box: the card title says what's plotted.
    <div className="chart-box">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={{ top: 8, right: 12, bottom: 4, left: 4 }}>
          <CartesianGrid stroke="var(--gridline)" strokeWidth={1} vertical={false} />
          <XAxis
            dataKey="index"
            stroke="var(--baseline)"
            tick={{ fill: 'var(--text-muted)', fontSize: 11 }}
            tickLine={false}
            // Thin the ticks out; the default packs them shoulder-to-shoulder over
            // a few thousand trades.
            minTickGap={56}
            label={{
              value: 'Trade number',
              position: 'insideBottom',
              offset: -2,
              fill: 'var(--text-muted)',
              fontSize: 11,
            }}
            height={36}
          />
          <YAxis
            stroke="var(--baseline)"
            tick={{ fill: 'var(--text-muted)', fontSize: 11 }}
            tickLine={false}
            width={64}
            tickFormatter={(v: number) => v.toLocaleString()}
          />
          <Tooltip
            content={<EquityTooltip currency={currency} />}
            cursor={{ stroke: 'var(--baseline)', strokeWidth: 1 }}
          />
          <Area
            type="monotone"
            dataKey="cumulativePnL"
            stroke="var(--series-1)"
            strokeWidth={2}
            strokeLinejoin="round"
            strokeLinecap="round"
            fill="var(--series-1)"
            fillOpacity={0.1}
            dot={false}
            activeDot={{ r: 4, strokeWidth: 2, stroke: 'var(--surface-1)' }}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  )
}
