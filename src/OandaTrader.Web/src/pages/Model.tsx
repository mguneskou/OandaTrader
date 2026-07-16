import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { StatTile } from '../components/StatTile'
import { dateTime } from '../lib/format'

interface ParsedMetrics {
  metrics?: { auc?: number | null; accuracy?: number; f1?: number; error?: string }
  comparison?: { newScore?: number | null; oldScore?: number | null; promoted?: boolean }
  split?: { train?: number; test?: number }
}

function parseMetrics(json: string): ParsedMetrics {
  try {
    return JSON.parse(json) as ParsedMetrics
  } catch {
    return {}
  }
}

function fmtScore(v: number | null | undefined): string {
  return v == null ? '—' : v.toFixed(3)
}

export function Model() {
  const queryClient = useQueryClient()
  const [instrument, setInstrument] = useState('EUR_USD')
  const [months, setMonths] = useState(6)
  const [notice, setNotice] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const status = useQuery({ queryKey: ['modelStatus'], queryFn: api.modelStatus })
  const versions = useQuery({ queryKey: ['modelVersions'], queryFn: api.modelVersions })
  const settings = useQuery({ queryKey: ['settings'], queryFn: api.getSettings })

  const refreshAll = () => {
    void queryClient.invalidateQueries({ queryKey: ['modelStatus'] })
    void queryClient.invalidateQueries({ queryKey: ['modelVersions'] })
    void queryClient.invalidateQueries({ queryKey: ['trades'] })
    void queryClient.invalidateQueries({ queryKey: ['analytics'] })
  }

  const backtest = useMutation({
    mutationFn: () => api.runBacktest(instrument, months),
    onMutate: () => {
      setError(null)
      setNotice(null)
    },
    onSuccess: (r) => {
      setNotice(
        `Backtest ${r.instrument} (${r.granularity}, ${months}mo): ${r.summary.tradeCount} trades, ` +
          `${r.summary.winRatePercent.toFixed(1)}% win rate, ${r.summary.totalPnLInR.toFixed(1)}R total.`,
      )
      refreshAll()
    },
    onError: (e: Error) => setError(e.message),
  })

  const train = useMutation({
    mutationFn: api.trainModel,
    onMutate: () => {
      setError(null)
      setNotice(null)
    },
    onSuccess: (r) => {
      setNotice(
        `Trained model #${r.id} on ${r.trainingSampleCount} samples. ` +
          (r.promoted
            ? 'Promoted to active — the engine will use it for new signals.'
            : 'Not promoted: it scored worse than the current active model on the hold-out slice.'),
      )
      refreshAll()
    },
    onError: (e: Error) => setError(e.message),
  })

  const activeVersion = versions.data?.find((v) => v.isActive)
  const activeMetrics = activeVersion ? parseMetrics(activeVersion.metricsJson) : null
  const instruments = settings.data?.instruments ?? []

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Model</h1>
          <div className="page-subtitle">
            Backtests generate training data; training fits a win-probability model the engine
            uses to gate signals.
          </div>
        </div>
      </div>

      {error && (
        <div className="banner banner-critical">
          <span className="banner-icon" aria-hidden="true">!</span>
          <div>{error}</div>
        </div>
      )}
      {notice && (
        <div className="banner banner-good">
          <span className="banner-icon" aria-hidden="true">✓</span>
          <div>{notice}</div>
        </div>
      )}

      <div className="grid grid-kpi">
        <StatTile
          label="Predictor"
          value={status.data?.ready ? 'Ready' : 'Not ready'}
          delta={status.data?.ready ? 'Engine can score signals' : 'Train a model to enable trading'}
        />
        <StatTile
          label="Active model"
          value={activeVersion ? `#${activeVersion.id}` : '—'}
          delta={activeVersion ? dateTime(activeVersion.trainedAtUtc) : undefined}
        />
        <StatTile
          label="Training samples"
          value={activeVersion?.trainingSampleCount ?? '—'}
          delta={
            activeMetrics?.split
              ? `${activeMetrics.split.train} train / ${activeMetrics.split.test} test`
              : undefined
          }
        />
        <StatTile
          label="Hold-out AUC"
          value={fmtScore(activeMetrics?.metrics?.auc)}
          delta={
            activeMetrics?.metrics?.accuracy != null
              ? `${(activeMetrics.metrics.accuracy * 100).toFixed(1)}% accuracy`
              : undefined
          }
        />
      </div>

      <div className="grid grid-2">
        <div className="card">
          <div className="card-title">1 · Generate training data</div>
          <div className="card-subtitle">
            Replays the baseline strategy over historical candles at your configured granularity
            ({settings.data?.settings.granularity ?? '…'}).
          </div>

          <div className="field">
            <label htmlFor="bt-instrument">Instrument</label>
            <select
              id="bt-instrument"
              value={instrument}
              onChange={(e) => setInstrument(e.target.value)}
            >
              {instruments.map((i) => (
                <option key={i.instrument} value={i.instrument}>
                  {i.instrument}
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label htmlFor="bt-months">History (months)</label>
            <input
              id="bt-months"
              type="number"
              min={1}
              max={24}
              value={months}
              onChange={(e) => setMonths(Number(e.target.value))}
            />
            <div className="field-hint">More history means more training samples, but a slower run.</div>
          </div>

          <button
            className="btn btn-primary"
            onClick={() => backtest.mutate()}
            disabled={backtest.isPending || instruments.length === 0}
          >
            {backtest.isPending ? 'Running backtest…' : 'Run backtest'}
          </button>
        </div>

        <div className="card">
          <div className="card-title">2 · Train a model</div>
          <div className="card-subtitle">
            Fits on every labelled trade (backtest + live), evaluates on a chronological hold-out
            slice, and only promotes the result if it isn't worse than the current model.
          </div>
          <button
            className="btn btn-primary"
            onClick={() => train.mutate()}
            disabled={train.isPending}
          >
            {train.isPending ? 'Training…' : 'Train model now'}
          </button>
          <div className="field-hint" style={{ marginTop: '0.5rem' }}>
            The engine also retrains automatically after every{' '}
            {settings.data?.settings.retrainAfterTradeCount ?? '…'} closed live trades.
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-title">Model versions</div>
        {versions.data && versions.data.length > 0 ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Trained</th>
                  <th className="num">#</th>
                  <th>Active</th>
                  <th className="num">Samples</th>
                  <th className="num">AUC</th>
                  <th className="num">Accuracy</th>
                  <th className="num">Prev score</th>
                  <th>Promoted</th>
                </tr>
              </thead>
              <tbody>
                {versions.data.map((v) => {
                  const m = parseMetrics(v.metricsJson)
                  return (
                    <tr key={v.id}>
                      <td>{dateTime(v.trainedAtUtc)}</td>
                      <td className="num">{v.id}</td>
                      <td>{v.isActive ? 'Active' : '—'}</td>
                      <td className="num">{v.trainingSampleCount}</td>
                      <td className="num">{fmtScore(m.metrics?.auc)}</td>
                      <td className="num">
                        {m.metrics?.accuracy != null ? `${(m.metrics.accuracy * 100).toFixed(1)}%` : '—'}
                      </td>
                      <td className="num">{fmtScore(m.comparison?.oldScore)}</td>
                      <td>{m.comparison?.promoted ? 'Yes' : 'No'}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty">No models trained yet.</div>
        )}
      </div>
    </>
  )
}
