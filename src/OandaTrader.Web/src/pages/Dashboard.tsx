import { useEffect } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { useEngineHub } from '../hooks/EngineHubProvider'
import { StatTile } from '../components/StatTile'
import { money, percent, pnlClass, price, probabilityPercent, signedMoney, timeOnly } from '../lib/format'

export function Dashboard() {
  const { prices, account, engineStatus, logs, tradeEventCount } = useEngineHub()
  const queryClient = useQueryClient()

  const accountQuery = useQuery({ queryKey: ['account'], queryFn: api.accountSummary, retry: false })
  const openTrades = useQuery({ queryKey: ['openTrades'], queryFn: api.openTrades })
  const analytics = useQuery({ queryKey: ['analytics', 'Live'], queryFn: () => api.analytics('Live') })
  const modelStatus = useQuery({ queryKey: ['modelStatus'], queryFn: api.modelStatus })

  // A trade opening/closing invalidates both the open-positions list and the stats.
  useEffect(() => {
    if (tradeEventCount === 0) return
    void queryClient.invalidateQueries({ queryKey: ['openTrades'] })
    void queryClient.invalidateQueries({ queryKey: ['analytics'] })
  }, [tradeEventCount, queryClient])

  const invalidateEngine = () => {
    void queryClient.invalidateQueries({ queryKey: ['engineStatus'] })
  }
  const startMutation = useMutation({ mutationFn: api.engineStart, onSuccess: invalidateEngine })
  const stopMutation = useMutation({ mutationFn: api.engineStop, onSuccess: invalidateEngine })
  const resumeMutation = useMutation({ mutationFn: api.engineResume, onSuccess: invalidateEngine })

  const currency = accountQuery.data?.account.currency ?? ''
  // Prefer the live SignalR push; fall back to the REST snapshot before the first tick.
  const nav = account?.nav ?? accountQuery.data?.account.nav
  const balance = account?.balance ?? accountQuery.data?.account.balance
  const unrealized = account?.unrealizedPl ?? accountQuery.data?.account.unrealizedPl
  const openCount = account?.openTradeCount ?? accountQuery.data?.account.openTradeCount

  const paused = Boolean(engineStatus?.pausedReason)
  const running = engineStatus?.engineEnabled === true

  const priceRows = Object.values(prices).sort((a, b) => a.instrument.localeCompare(b.instrument))

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Dashboard</h1>
          <div className="page-subtitle">
            Oanda practice account {accountQuery.data?.account.id ?? ''}
          </div>
        </div>
        <div className="btn-row">
          {running ? (
            <button
              className="btn btn-danger"
              onClick={() => stopMutation.mutate()}
              disabled={stopMutation.isPending}
            >
              Stop engine
            </button>
          ) : (
            <button
              className="btn btn-primary"
              onClick={() => startMutation.mutate()}
              disabled={startMutation.isPending}
            >
              Start engine
            </button>
          )}
        </div>
      </div>

      {accountQuery.isError && (
        <div className="banner banner-critical">
          <span className="banner-icon" aria-hidden="true">!</span>
          <div>
            <strong>Cannot reach the Oanda account.</strong>{' '}
            {(accountQuery.error as Error).message}
          </div>
        </div>
      )}

      {paused && (
        <div className="banner banner-critical">
          <span className="banner-icon" aria-hidden="true">!</span>
          <div style={{ flex: 1 }}>
            <strong>Trading paused by a circuit breaker.</strong> {engineStatus?.pausedReason}
            <div style={{ marginTop: 8 }}>
              <button
                className="btn"
                onClick={() => resumeMutation.mutate()}
                disabled={resumeMutation.isPending}
              >
                Resume trading
              </button>
            </div>
          </div>
        </div>
      )}

      {modelStatus.data && !modelStatus.data.ready && (
        <div className="banner">
          <span className="banner-icon" aria-hidden="true">i</span>
          <div>
            <strong>No trained model yet.</strong> The engine won't open trades until one exists.
            Run a backtest on the Model page to generate training data, then train.
          </div>
        </div>
      )}

      <div className="grid grid-kpi">
        <StatTile
          label={`Account value (NAV, ${currency})`}
          value={money(nav)}
          hero
          delta={unrealized != null ? `${signedMoney(unrealized)} unrealized` : undefined}
          deltaClass={pnlClass(unrealized)}
        />
        <StatTile label={`Balance (${currency})`} value={money(balance)} />
        <StatTile label="Open positions" value={openCount ?? '—'} />
        <StatTile
          label="Live win rate"
          value={analytics.data ? percent(analytics.data.winRatePercent) : '—'}
          delta={
            analytics.data
              ? `${analytics.data.wins}W / ${analytics.data.losses}L of ${analytics.data.tradeCount}`
              : undefined
          }
        />
        <StatTile
          label={`Live realized P&L (${currency})`}
          value={analytics.data ? signedMoney(analytics.data.totalPnL) : '—'}
          deltaClass={pnlClass(analytics.data?.totalPnL)}
        />
      </div>

      <div className="grid grid-2">
        <div className="card">
          <div className="card-title">Live prices</div>
          <div className="card-subtitle">Streamed from Oanda</div>
          {priceRows.length === 0 ? (
            <div className="empty">Waiting for the first tick…</div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Instrument</th>
                    <th className="num">Bid</th>
                    <th className="num">Ask</th>
                    <th className="num">Spread</th>
                    <th className="num">Updated</th>
                  </tr>
                </thead>
                <tbody>
                  {priceRows.map((p) => (
                    <tr key={p.instrument}>
                      <td>{p.instrument}</td>
                      <td className="num">{price(p.bid)}</td>
                      <td className="num">{price(p.ask)}</td>
                      <td className="num">{(p.ask - p.bid).toFixed(5)}</td>
                      <td className="num">{timeOnly(p.timeUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="card">
          <div className="card-title">Engine activity</div>
          <div className="card-subtitle">Live events since this page loaded</div>
          {logs.length === 0 ? (
            <div className="empty">No engine events yet.</div>
          ) : (
            <div className="log-feed">
              {logs.map((l, i) => (
                <div className="log-line" key={`${l.timestampUtc}-${i}`}>
                  <span className="log-time">{timeOnly(l.timestampUtc)}</span>
                  <span className="log-msg">{l.message}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="card">
        <div className="card-title">Open positions</div>
        {openTrades.data && openTrades.data.length > 0 ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Instrument</th>
                  <th>Direction</th>
                  <th className="num">Units</th>
                  <th className="num">Entry</th>
                  <th className="num">Stop</th>
                  <th className="num">Target</th>
                  <th className="num">Confidence</th>
                  <th>Why</th>
                </tr>
              </thead>
              <tbody>
                {openTrades.data.map((t) => (
                  <tr key={t.id}>
                    <td>{t.instrument}</td>
                    <td>{t.direction}</td>
                    <td className="num">{t.units.toLocaleString()}</td>
                    <td className="num">{price(t.entryPrice)}</td>
                    <td className="num">{price(t.stopLoss)}</td>
                    <td className="num">{price(t.takeProfit)}</td>
                    <td className="num">{probabilityPercent(t.mlConfidence)}</td>
                    <td className="wrap">{t.reasoningText}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty">No open positions.</div>
        )}
      </div>
    </>
  )
}
