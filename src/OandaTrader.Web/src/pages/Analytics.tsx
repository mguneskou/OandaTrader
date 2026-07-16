import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { StatTile } from '../components/StatTile'
import { EquityCurveChart } from '../components/charts/EquityCurveChart'
import { InstrumentPnLChart } from '../components/charts/InstrumentPnLChart'
import { dateTime, percent, pnlClass, signedMoney } from '../lib/format'

type SourceFilter = 'Live' | 'Backtest'
type CurveView = 'chart' | 'table'

export function Analytics() {
  const [source, setSource] = useState<SourceFilter>('Live')
  const [curveView, setCurveView] = useState<CurveView>('chart')

  const analytics = useQuery({
    queryKey: ['analytics', source],
    queryFn: () => api.analytics(source),
  })
  const accountQuery = useQuery({ queryKey: ['account'], queryFn: api.accountSummary, retry: false })
  const breakerEvents = useQuery({
    queryKey: ['circuitBreakerEvents'],
    queryFn: api.circuitBreakerEvents,
  })

  // Backtest P&L is in R-multiples (risk units), not account currency.
  const currency = source === 'Backtest' ? 'R' : (accountQuery.data?.account.currency ?? '')
  const data = analytics.data

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Analytics</h1>
          <div className="page-subtitle">
            {source === 'Live'
              ? 'Performance of trades placed on the practice account.'
              : 'Performance of the backtest dataset, in R-multiples.'}
          </div>
        </div>
        <div className="toggle-group">
          {(['Live', 'Backtest'] as SourceFilter[]).map((s) => (
            <button key={s} className={source === s ? 'active' : ''} onClick={() => setSource(s)}>
              {s}
            </button>
          ))}
        </div>
      </div>

      {analytics.isError && (
        <div className="banner banner-critical">
          <span className="banner-icon" aria-hidden="true">!</span>
          <div>Failed to load analytics: {(analytics.error as Error).message}</div>
        </div>
      )}

      <div className="grid grid-kpi">
        <StatTile label="Closed trades" value={data?.tradeCount ?? '—'} />
        <StatTile
          label="Win rate"
          value={data ? percent(data.winRatePercent) : '—'}
          delta={data ? `${data.wins}W / ${data.losses}L` : undefined}
        />
        <StatTile
          label={`Total P&L (${currency})`}
          value={data ? signedMoney(data.totalPnL) : '—'}
          deltaClass={pnlClass(data?.totalPnL)}
        />
        <StatTile
          label="Instruments traded"
          value={data?.perInstrument.length ?? '—'}
        />
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: '1rem' }}>
          <div>
            <div className="card-title">Cumulative P&L ({currency})</div>
            <div className="card-subtitle">Running total across closed trades, in sequence</div>
          </div>
          {/* Table-view twin: every value in the chart is reachable without color. */}
          <div className="toggle-group">
            {(['chart', 'table'] as CurveView[]).map((v) => (
              <button key={v} className={curveView === v ? 'active' : ''} onClick={() => setCurveView(v)}>
                {v === 'chart' ? 'Chart' : 'Table'}
              </button>
            ))}
          </div>
        </div>

        {curveView === 'chart' ? (
          <EquityCurveChart data={data?.equityCurve ?? []} currency={currency} />
        ) : (
          <div className="table-wrap" style={{ maxHeight: 260, overflowY: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th className="num">Trade</th>
                  <th>Closed</th>
                  <th className="num">Cumulative P&L ({currency})</th>
                </tr>
              </thead>
              <tbody>
                {(data?.equityCurve ?? []).map((p) => (
                  <tr key={p.index}>
                    <td className="num">{p.index}</td>
                    <td>{dateTime(p.time)}</td>
                    <td className={`num ${pnlClass(p.cumulativePnL)}`}>{signedMoney(p.cumulativePnL)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {(data?.equityCurve.length ?? 0) === 0 && <div className="empty">No closed trades yet.</div>}
          </div>
        )}
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div className="card-title">P&L by instrument ({currency})</div>
        <div className="card-subtitle">Bars above the line are net winners; below, net losers</div>
        <InstrumentPnLChart data={data?.perInstrument ?? []} currency={currency} />

        {(data?.perInstrument.length ?? 0) > 0 && (
          <div className="table-wrap" style={{ marginTop: '0.75rem' }}>
            <table>
              <thead>
                <tr>
                  <th>Instrument</th>
                  <th className="num">Trades</th>
                  <th className="num">Wins</th>
                  <th className="num">Win rate</th>
                  <th className="num">Total P&L ({currency})</th>
                </tr>
              </thead>
              <tbody>
                {data!.perInstrument.map((row) => (
                  <tr key={row.instrument}>
                    <td>{row.instrument}</td>
                    <td className="num">{row.trades}</td>
                    <td className="num">{row.wins}</td>
                    <td className="num">{percent(row.winRatePercent)}</td>
                    <td className={`num ${pnlClass(row.totalPnL)}`}>{signedMoney(row.totalPnL)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="card">
        <div className="card-title">Circuit breaker events</div>
        {breakerEvents.data && breakerEvents.data.length > 0 ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>When</th>
                  <th>Limit</th>
                  <th>Reason</th>
                </tr>
              </thead>
              <tbody>
                {breakerEvents.data.map((e) => (
                  <tr key={e.id}>
                    <td>{dateTime(e.timestampUtc)}</td>
                    <td>{e.triggeredLimit}</td>
                    <td className="wrap">{e.reason}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty">No circuit breaker has tripped.</div>
        )}
      </div>
    </>
  )
}
