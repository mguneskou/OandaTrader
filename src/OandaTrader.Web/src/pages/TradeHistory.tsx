import { useEffect, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { useEngineHub } from '../hooks/EngineHubProvider'
import { dateTime, pnlClass, price, probabilityPercent, signedMoney } from '../lib/format'

type SourceFilter = 'Live' | 'Backtest'

export function TradeHistory() {
  const [source, setSource] = useState<SourceFilter>('Live')
  const { tradeEventCount } = useEngineHub()
  const queryClient = useQueryClient()

  const trades = useQuery({
    queryKey: ['trades', source],
    queryFn: () => api.trades(source, 200),
  })

  useEffect(() => {
    if (tradeEventCount === 0) return
    void queryClient.invalidateQueries({ queryKey: ['trades'] })
  }, [tradeEventCount, queryClient])

  return (
    <>
      {/* One filter row above everything it scopes. */}
      <div className="page-header">
        <div>
          <h1>Trades</h1>
          <div className="page-subtitle">
            {source === 'Live'
              ? 'Trades placed on the Oanda practice account.'
              : 'Synthetic trades from backtests — the ML training data.'}
          </div>
        </div>
        <div className="toggle-group">
          {(['Live', 'Backtest'] as SourceFilter[]).map((s) => (
            <button
              key={s}
              className={source === s ? 'active' : ''}
              onClick={() => setSource(s)}
            >
              {s}
            </button>
          ))}
        </div>
      </div>

      <div className="card">
        {trades.isLoading && <div className="empty">Loading…</div>}
        {trades.isError && (
          <div className="empty">Failed to load trades: {(trades.error as Error).message}</div>
        )}
        {trades.data && trades.data.length === 0 && (
          <div className="empty">
            No {source.toLowerCase()} trades yet.
            {source === 'Backtest' && ' Run a backtest from the Model page.'}
          </div>
        )}
        {trades.data && trades.data.length > 0 && (
          <div
            className="table-wrap"
            // Hold the previous render at reduced opacity on refetch — no skeleton flash.
            style={{ opacity: trades.isFetching ? 0.6 : 1 }}
          >
            <table>
              <thead>
                <tr>
                  <th>Opened</th>
                  <th>Instrument</th>
                  <th>Direction</th>
                  <th className="num">Entry</th>
                  <th className="num">Exit</th>
                  <th>Outcome</th>
                  <th className="num">{source === 'Backtest' ? 'P&L (R)' : 'P&L'}</th>
                  <th className="num">Confidence</th>
                  <th>Why</th>
                </tr>
              </thead>
              <tbody>
                {trades.data.map((t) => (
                  <tr key={t.id}>
                    <td>{dateTime(t.entryTimeUtc)}</td>
                    <td>{t.instrument}</td>
                    <td>{t.direction}</td>
                    <td className="num">{price(t.entryPrice)}</td>
                    <td className="num">{price(t.exitPrice)}</td>
                    <td>{t.outcome}</td>
                    <td className={`num ${pnlClass(t.pnL)}`}>{signedMoney(t.pnL)}</td>
                    <td className="num">{probabilityPercent(t.mlConfidence)}</td>
                    <td className="wrap">{t.reasoningText}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}
